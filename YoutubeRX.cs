using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;

namespace treciProjekat;

public class YoutubeRxStreamer
{
    private readonly IActorRef _videoManager;
    private readonly string[] _videoIds; // Lista ID-jeva koje tvoja aplikacija prati

    public YoutubeRxStreamer(IActorRef videoManager, string[] videoIds)
    {
        _videoManager = videoManager;
        _videoIds = videoIds;
    }

    public void PokreniPeriodicnoOsvezavanje()
    {
        // 1. Postavljamo tajmer (npr. okidaj na svakih 10 minuta)
        Observable.Interval(TimeSpan.FromMinutes(10))
            // StartWith(0) obezbeđuje da povuče komentare odmah pri paljenju, da ne čekaš 10 minuta
            .StartWith(0)

            // 2. Za svaki kucaj tajmera, prođi kroz sve video snimke iz niza
            .SelectMany(_ => _videoIds)

            // 3. Pozivamo tvoju pravu asinhranu metodu iz YouTubeExample klase.
            // Rx.NET će ovde automatski "sačekati" (await-ovati) Task i proslediti rezultat dalje.
            .SelectMany(async videoId =>
            {
                List<string> komentari = await YouTubeExample.GetVideoCommentsAsync(videoId);

                // Pakujemo rezultat u anonimni objekat da bismo u sledećem koraku znali 
                // koji tekstovi pripadaju kom videoId-ju.
                return new { VideoId = videoId, Tekstovi = komentari };
            })

            // 4. Emitovanje i slanje aktorima
            .Subscribe(
                rezultat =>
                {
                    // Prolazimo kroz listu stringova koju nam je vratio YouTubeExample
                    foreach (var tekst in rezultat.Tekstovi)
                    {
                        // Osnovno filtriranje (profesorov zahtev) - preskačemo ako je prazno
                        if (string.IsNullOrWhiteSpace(tekst)) continue;

                        // Mapiranje u poruku relevantnu za aktore (profesorov zahtev)
                        var poruka = new NoviKomentarPoruka(rezultat.VideoId, tekst);

                        // Šaljemo menadžeru, a on prosleđuje detetu-aktoru
                        _videoManager.Tell(poruka);
                    }

                    Console.WriteLine($"[Rx.NET] Povučeno i prosleđeno {rezultat.Tekstovi.Count} komentara za video: {rezultat.VideoId}");
                },
                greška =>
                {
                    Console.WriteLine($"Greška u Rx toku: {greška.Message}");
                }
            );
    }
}