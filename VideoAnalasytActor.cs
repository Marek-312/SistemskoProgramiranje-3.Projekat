namespace treciProjekat;


public class VideoAnalystActor : ReceiveActor
{
    //public readonly string _videoID;
    //private readonly ML _ml;
    public int pozitivniBrojac;
    public int negativniBrojac;
    public VideoAnalystActor(string videoID)
    {
        //_ml = new ML();

        pozitivniBrojac = 0;
        negativniBrojac = 0;
        Receive<ZapocniAnalizuPoruka>(text =>
        {
            if (ML.AnalizirajTekst(text.VideoId))
            {
                pozitivniBrojac++;

            }
            else { negativniBrojac--; }
            _sviKomentari.Add(text.VideoId);
        });
    }

}