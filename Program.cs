using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Akka.Actor;
using treciProjekat;

namespace treciProjekat;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== STARTAP APLIKACIJE ===");

        // ==========================================
        // 1. INICIJALIZACIJA ML.NET MODELA
        // ==========================================
        Console.WriteLine("1. Inicijalizacija i treniranje ML.NET modela...");
        ML.Inicijalizuj();

        // ==========================================
        // 2. INICIJALIZACIJA AKKA.NET AKTORA
        // ==========================================
        Console.WriteLine("2. Pokretanje Akka.NET aktorskog sistema...");
        var actorSystem = ActorSystem.Create("SentimentAnalysisSystem");
        IActorRef videoManagerActor = actorSystem.ActorOf(Props.Create(() => new VideoManagerActor()), "videoManager");

        // ==========================================
        // 3. POKRETANJE RX.NET POZADINSKOG TOKA
        // ==========================================
        Console.WriteLine("3. Pokretanje periodičnog Rx.NET toka podataka...");
        var videoIdsZaPracenje = new[] { "dQw4w9WgXcQ", "v_09GAPVE9w" }; // ID-jevi koje pratimo

        var rxStreamer = new YoutubeRxStreamer(videoManagerActor, videoIdsZaPracenje);
        rxStreamer.PokreniPeriodicnoOsvezavanje(); // Ovo radi asinhrono u pozadini!

        // ==========================================
        // 4. RUČNO PODIZANJE HTTP SERVERA (HttpListener)
        // ==========================================
        Console.WriteLine("4. Pokretanje HTTP servera...");
        HttpListener listener = new HttpListener();

        // Slušamo na portu 5000 (možeš promeniti port ako želiš)
        listener.Prefixes.Add("http://localhost:5000/");
        listener.Start();
        Console.WriteLine("-> Server uspešno sluša na adresi: http://localhost:5000/");
        Console.WriteLine("Aplikacija je spremna. Pritisni CTRL+C za izlaz.");

        // Beskonačna petlja koja menja Web API Kontrolere
        while (true)
        {
            try
            {
                // Čekamo dolazni HTTP zahtev (ne blokira pozadinske niti i Rx)
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                // Očekivani URL format: http://localhost:5000/?videoId=dQw4w9WgXcQ
                string videoId = request.QueryString["videoId"];

                if (string.IsNullOrEmpty(videoId))
                {
                    // Ako korisnik nije prosledio videoId parametar
                    VratiGresku(response, 400, "Morate proslediti videoId kroz Query string! (npr. /?videoId=dQw4w9WgXcQ)");
                    continue;
                }

                // 5. KOMUNIKACIJA SA AKTORIMA (ZAMENA ZA KONTROLER)
                // Pitamo aktorski sistem za trenutno stanje u memoriji
                var stanje = await videoManagerActor.Ask<RezultatSentimentAnalize>(new DajTrenutnoStanjePoruka(videoId));

                // Serijalizujemo objekat u JSON string
                string jsonOdgovor = JsonSerializer.Serialize(stanje);
                byte[] buffer = Encoding.UTF8.GetBytes(jsonOdgovor);

                // Slanje JSON-a nazad klijentu (pretraživaču)
                response.ContentType = "application/json";
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška prilikom obrade HTTP zahteva: {ex.Message}");
            }
        }
    }

    // Pomoćna metoda za vraćanje HTTP grešaka
    static void VratiGresku(HttpListenerResponse response, int statusCode, string poruka)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { greska = poruka }));
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }
}