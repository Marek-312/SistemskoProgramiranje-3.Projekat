using Microsoft.ML;
using Microsoft.ML.Data;
using static Microsoft.ML.DataOperationsCatalog;

namespace treciProjekat
{
    public class SentimentData
    {
        [LoadColumn(0)]
        public string? SentimentText;

        [LoadColumn(1), ColumnName("Label")]
        public bool Sentiment;
    }

    public class SentimentPrediction : SentimentData
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }

        public float Probability { get; set; }

        public float Score { get; set; }
    }

    public class ML
    {
        private static MLContext _mlContext = new MLContext();
        private static ITransformer _model;
        private static PredictionEngine<SentimentData, SentimentPrediction> _predictionEngine;
        public static void Inicijalizuj()
        {
            TrainTestData splitDataView = LoadData(_mlContext);
            _model = BuildAndTrainModel(_mlContext, splitDataView.TrainSet);
            Evaluate(_mlContext, _model, splitDataView.TestSet);

            // Kreiramo engine koji će aktori koristiti
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(_model);
        }
        public static bool AnalizirajTekst(string tekst)
        {
            var input = new SentimentData { SentimentText = tekst };

            // PredictionEngine NIJE thread-safe u ML.NET-u ako se koristi grubo, 
            // ali pošto naši aktori rade single-threaded, zaključavanje (lock)
            // obezbeđuje sigurnost ako više aktora pristupa u isto vreme.
            lock (_predictionEngine)
            {
                var prediction = _predictionEngine.Predict(input);
                return prediction.Prediction; // vraća true (pozitivan) ili false (negativan)
            }
        }
        static string _dataPath = Path.Combine(
            Environment.CurrentDirectory,
            "data",
            "sentiment labelled sentences",
            "sentiment labelled sentences",
            "yelp_labelled.txt");

        public static void Run()
        {
            MLContext mlContext = new MLContext();

            // 1. Ucitaj podatke
            TrainTestData splitDataView = LoadData(mlContext);

            // 2. Treniraj model
            ITransformer model = BuildAndTrainModel(mlContext, splitDataView.TrainSet);

            // 3. Evaluiraj model
            Evaluate(mlContext, model, splitDataView.TestSet);
        }

        static TrainTestData LoadData(MLContext mlContext)
        {
            IDataView dataView = mlContext.Data.LoadFromTextFile<SentimentData>(
                _dataPath, hasHeader: false);

            TrainTestData splitDataView = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            return splitDataView;
        }

        static ITransformer BuildAndTrainModel(MLContext mlContext, IDataView splitTrainSet)
        {
            var estimator = mlContext.Transforms.Text
                .FeaturizeText(
                    outputColumnName: "Features",
                    inputColumnName: nameof(SentimentData.SentimentText))
                .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                    labelColumnName: "Label",
                    featureColumnName: "Features"));

            Console.WriteLine("=============== Create and Train the Model ===============");
            var model = estimator.Fit(splitTrainSet);
            Console.WriteLine("=============== End of training ===============");
            Console.WriteLine();

            return model;
        }

        static void Evaluate(MLContext mlContext, ITransformer model, IDataView splitTestSet)
        {
            Console.WriteLine("=============== Evaluating Model accuracy with Test data ===============");

            IDataView predictions = model.Transform(splitTestSet);
            var metrics = mlContext.BinaryClassification.Evaluate(predictions, "Label");

            Console.WriteLine();
            Console.WriteLine("Model quality metrics evaluation");
            Console.WriteLine("--------------------------------");
            Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
            Console.WriteLine($"Auc: {metrics.AreaUnderRocCurve:P2}");
            Console.WriteLine($"F1Score: {metrics.F1Score:P2}");
            Console.WriteLine("=============== End of model evaluation ===============");
        }
    }
}