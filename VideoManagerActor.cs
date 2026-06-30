using Akka.Actor;
using System.Collections.Generic;

namespace treciProjekat;

public class VideoManagerActor : ReceiveActor
{
    private readonly Dictionary<string, IActorRef> _videoAktori;

    public VideoManagerActor()
    {
        _videoAktori = new Dictionary<string, IActorRef>();

        // 1. Reagovanje na zahtev sa Weba (zahtev za stanje)
        Receive<DajTrenutnoStanjePoruka>(poruka =>
        {
            string videoId = poruka.VideoId;
            ProveriIKreirajDete(videoId);

            // Forward-ujemo zahtev detetu da ono direktno odgovori HttpListener-u
            _videoAktori[videoId].Forward(poruka);
        });

        // 2. EVO GDE JE BIO PROBLEM: Dodajemo reagovanje na komentar iz Rx.NET-a!
        Receive<NoviKomentarPoruka>(poruka =>
        {
            string videoId = poruka.VideoId;
            ProveriIKreirajDete(videoId);

            // Prosleđujemo komentar detetu koje ga analizira preko ML.NET-a
            _videoAktori[videoId].Tell(poruka);
        });
    }

    // Pomoćna metoda unutar aktora da ne dupliramo kod za pravljenje dece
    private void ProveriIKreirajDete(string videoId)
    {
        if (!_videoAktori.ContainsKey(videoId))
        {
            IActorRef deteAktor = Context.ActorOf(
                Props.Create(() => new VideoAnalystActor(videoId)),
                $"analyst-{videoId}"
            );
            _videoAktori.Add(videoId, deteAktor);
        }
    }
}