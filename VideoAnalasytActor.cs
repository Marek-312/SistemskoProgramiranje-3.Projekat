using Akka.Actor;
using System.Collections.Generic;

namespace treciProjekat;

// Definisanje poruka koje ovaj aktor prima i šalje
public record NoviKomentarPoruka(string VideoId, string TekstKomentara);
public record DajTrenutnoStanjePoruka(string VideoId);
public record RezultatSentimentAnalize(int Pozitivni, int Negativni, double UkupnoPozitivnihProcenat);

public class VideoAnalystActor : ReceiveActor
{
    private readonly string _videoId;

    // Interno stanje aktora
    private int _pozitivniBrojac = 0;
    private int _negativniBrojac = 0;
    private readonly List<string> _sviKomentari = new();

    public VideoAnalystActor(string videoId)
    {
        _videoId = videoId;

        // Situacija 1: Rx.NET je poslao novi komentar sa YouTube-a
        Receive<NoviKomentarPoruka>(poruka =>
        {
            // Pozivamo tvoju ML klasu za analizu
            bool jePozitivan = ML.AnalizirajTekst(poruka.TekstKomentara);

            if (jePozitivan)
                _pozitivniBrojac++;
            else
                _negativniBrojac++;

            _sviKomentari.Add(poruka.TekstKomentara);
        });

        // Situacija 2: Web kontroler pita "Kakvo je stanje?"
        Receive<DajTrenutnoStanjePoruka>(poruka =>
        {
            int ukupno = _pozitivniBrojac + _negativniBrojac;
            double procenat = ukupno > 0 ? ((double)_pozitivniBrojac / ukupno) * 100 : 0;

            // Pakujemo stanje i šaljemo nazad pošiljaocu (Web kontroleru)
            var odgovor = new RezultatSentimentAnalize(_pozitivniBrojac, _negativniBrojac, procenat);
            Sender.Tell(odgovor);
        });
    }
}