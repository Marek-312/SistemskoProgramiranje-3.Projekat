using Akka.Actor;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace treciProjekat;

// Definišemo poruke koje menadžer razume
public record ZapocniAnalizuPoruka(string VideoId);
public record ObrišiVideoPoruka(string VideoId);

public class VideoManagerActor : ReceiveActor
{
    // Interno stanje - običan rečnik koji je bezbedan jer ga samo ovaj aktor vidi
    private readonly Dictionary<string, IActorRef> _videoAktori;

    public VideoManagerActor()
    {
        _videoAktori = new Dictionary<string, IActorRef>();

        // 1. Šta radimo kada stigne zahtev za analizu (iz kontrolera ili Rx-a)
        Receive<ZapocniAnalizuPoruka>(poruka =>
        {
            string videoId = poruka.VideoId;

            // Ako aktor za taj video već ne postoji, pravimo ga dinamički!
            if (!_videoAktori.ContainsKey(videoId))
            {
                // Props govori Akka.NET-u kako da instancira tvoje dete-aktora
                IActorRef deteAktor = Context.ActorOf(Props.Create(() => new VideoAnalystActor(videoId)), $"analyst-{videoId}");

                _videoAktori.Add(videoId, deteAktor);
            }

            // Prosleđujemo (Forward) poruku detetu koje je zaduženo za taj video.
            // Forward zadržava originalnog pošiljaoca (Web server), pa će dete moći direktno da mu odgovori.
            _videoAktori[videoId].Forward(poruka);
        });

        // 2. Šta radimo ako želimo da obrišemo/zaustavimo praćenje videa
        Receive<ObrišiVideoPoruka>(poruka =>
        {
            if (_videoAktori.TryGetValue(poruka.VideoId, out var deteAktor))
            {
                // Pošaljemo detetu poruku da se ugasi (otruje)
                deteAktor.Tell(PoisonPill.Instance);

                // Obrišemo ga iz rečnika
                _videoAktori.Remove(poruka.VideoId);
            }
        });
    }
}