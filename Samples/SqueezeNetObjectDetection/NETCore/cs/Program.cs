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
using static Helpers.BlockTimerHelper;

namespace SampleModule
{
    class ImageInference
    {
        // globals
        private static readonly LearningModelDeviceKind _deviceKind = LearningModelDeviceKind.Default;
        private static readonly string _deviceName = "default";
        private static readonly string _labelsFileName = "Labels.json";
        private static AppOptions Options;

        static async Task<int> Main(string[] args)
        {
            try
            {
                //
                // Parse options
                //

                Options = new AppOptions();

                Options.Parse(args);

                if (Options.ShowList)
                    await Camera.EnumFrameSourcesAsync();

                if (Options.Exit)
                    return -1;

                //
                // Load model
                //

                ScoringModel __model = null;
                await BlockTimer($"Loading modelfile '{Options.ModelPath}' on the '{_deviceName}' device",
                    async () => {
                        StorageFile modelFile = AsyncHelper(StorageFile.GetFileFromPathAsync(Options.ModelPath));
                        __model = await ScoringModel.CreateFromStreamAsync(modelFile);
                    });

                //
                // Load & process image
                //
            
                StorageFile imageFile = AsyncHelper(StorageFile.GetFileFromPathAsync(Options.ImagePath));
                IRandomAccessStream stream = AsyncHelper(imageFile.OpenReadAsync());
                BitmapDecoder decoder = AsyncHelper(BitmapDecoder.CreateAsync(stream));
                SoftwareBitmap softwareBitmap = AsyncHelper(decoder.GetSoftwareBitmapAsync());
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                VideoFrame inputImage = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);
                ImageFeatureValue imageTensor = ImageFeatureValue.CreateFromVideoFrame(inputImage);

                //
                // Evaluate model
                //

                ScoringOutput outcome = null;
                await BlockTimer("Running the model",
                    async () => {
                        var input = new ScoringInput() { data_0 = imageTensor };
                        outcome = await __model.EvaluateAsync(input);
                    });

                //
                // Print results
                //

                var resultVector = outcome.softmaxout_1.GetAsVectorView();
                PrintResults(resultVector);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now.ToLocalTime()} ERROR: {ex.GetType().Name} {ex.Message}");
                return -1;
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

        private static void PrintResults(IReadOnlyList<float> resultVector)
        {
            // Parse labels from label json file.  We know the file's entries are already sorted in order.
            var fileString = File.ReadAllText(_labelsFileName);
            var fileDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileString);
            var labels = fileDict.Values.ToList();

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
                Console.WriteLine($"\"{ labels[topProbabilityLabelIndexes[i]]}\" with confidence of { topProbabilities[i]}");
            }
        }
    }
}