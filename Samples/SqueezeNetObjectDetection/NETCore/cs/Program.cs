using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.AI.MachineLearning;
using Windows.Foundation;
using Windows.Media;
using Newtonsoft.Json;

namespace SampleModule
{
    class ImageInference
    {
        // globals
        private static LearningModelDeviceKind _deviceKind = LearningModelDeviceKind.Default;
        private static string _deviceName = "default";
        private static string _modelPath;
        private static string _imagePath;
        private static string _labelsFileName = "Labels.json";
        private static List<string> _labels = new List<string>();
        private static AppOptions Options;

        private static SqueezeNetModel __model = null;

        // usage: SqueezeNet [modelfile] [imagefile] [cpu|directx]
        static async Task<int> Main(string[] args)
        {
            try
            {
                //
                // Parse options
                //

                Options = new AppOptions();

                Options.Parse(args);

                if (Options.Exit)
                    return -1;
                
                // Load and create the model 
                Console.WriteLine($"Loading modelfile '{Options.ModelPath}' on the '{_deviceName}' device");

                int ticks = Environment.TickCount;

                StorageFile modelFile = AsyncHelper(StorageFile.GetFileFromPathAsync(Options.ModelPath));
                __model = await SqueezeNetModel.CreateFromStreamAsync(modelFile);
                ticks = Environment.TickCount - ticks;
                Console.WriteLine($"model file loaded in { ticks } ticks");

                Console.WriteLine("Loading the image...");
                ImageFeatureValue imageTensor = LoadImageFile();

                Console.WriteLine("Running the model...");
                ticks = Environment.TickCount;

                var input = new SqueezeNetInput() { data_0 = imageTensor };
                var outcome = await __model.EvaluateAsync(input);

                ticks = Environment.TickCount - ticks;
                Console.WriteLine($"model run took { ticks } ticks");

                var resultTensor = outcome.softmaxout_1;

                // retrieve results from evaluation
                var resultVector = resultTensor.GetAsVectorView();
                PrintResults(resultVector);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now.ToLocalTime()} ERROR: {ex.GetType().Name} {ex.Message}");
                return -1;
            }
        }

        private static void LoadLabels()
        {
            // Parse labels from label json file.  We know the file's 
            // entries are already sorted in order.
            var fileString = File.ReadAllText(_labelsFileName);
            var fileDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileString);
            foreach (var kvp in fileDict)
            {
                _labels.Add(kvp.Value);
            }
        }

        
        private static T AsyncHelper<T> (IAsyncOperation<T> operation) 
        {
            AutoResetEvent waitHandle = new AutoResetEvent(false);
            operation.Completed = new AsyncOperationCompletedHandler<T>((op, status) =>
            {
                waitHandle.Set();
            });
            waitHandle.WaitOne();
            return operation.GetResults();
        }

        private static ImageFeatureValue LoadImageFile()
        {
            StorageFile imageFile = AsyncHelper(StorageFile.GetFileFromPathAsync(Options.ImagePath));
            IRandomAccessStream stream = AsyncHelper(imageFile.OpenReadAsync());
            BitmapDecoder decoder = AsyncHelper(BitmapDecoder.CreateAsync(stream));
            SoftwareBitmap softwareBitmap = AsyncHelper(decoder.GetSoftwareBitmapAsync());
            softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            VideoFrame inputImage = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);
            return ImageFeatureValue.CreateFromVideoFrame(inputImage);
        }


        private static void PrintResults(IReadOnlyList<float> resultVector)
        {
            // load the labels
            LoadLabels();
            // Find the top 3 probabilities
            List<float> topProbabilities = new List<float>() { 0.0f, 0.0f, 0.0f };
            List<int> topProbabilityLabelIndexes = new List<int>() { 0, 0, 0 };
            // SqueezeNet returns a list of 1000 options, with probabilities for each, loop through all
            for (int i = 0; i < resultVector.Count(); i++)
            {
                // is it one of the top 3?
                for (int j = 0; j < 3; j++)
                {
                    if (resultVector[i] > topProbabilities[j])
                    {
                        topProbabilityLabelIndexes[j] = i;
                        topProbabilities[j] = resultVector[i];
                        break;
                    }
                }
            }
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine($"\"{ _labels[topProbabilityLabelIndexes[i]]}\" with confidence of { topProbabilities[i]}");
            }
        }
    }
}