using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.AI.MachineLearning;
using Windows.Foundation;
using Windows.Media;

using static Helpers.BlockTimerHelper;
using static Helpers.AsyncHelper;
using SqueezeNetObjectDetectionNC;

namespace SampleModule
{
    class ImageInference
    {
        // globals
        private static readonly LearningModelDeviceKind _deviceKind = LearningModelDeviceKind.Default;
        private static readonly string _deviceName = "default";
        private static readonly string _labelsFileName = "Labels.json";
        private static AppOptions Options;
        private static ModuleClient ioTHubModuleClient;
        private static CancellationTokenSource cts = null;

        static async Task<int> Main(string[] args)
        {
            try
            {
                //
                // Parse options
                //

                Options = new AppOptions();

                Options.Parse(args);

                var devices = await Camera.EnumFrameSourcesAsync();
                if (Options.ShowList)
                    Camera.ListFrameSources(devices);                    

                if (Options.Exit)
                    return -1;

                //
                // Load model
                //

                ScoringModel __model = null;
                await BlockTimer($"Loading modelfile '{Options.ModelPath}' on the '{_deviceName}' device",
                    async () => {
                        var d = Directory.GetCurrentDirectory();
                        var path = d + "\\" + Options.ModelPath;
                        StorageFile modelFile = AsyncHelper(StorageFile.GetFileFromPathAsync(path));
                        __model = await ScoringModel.CreateFromStreamAsync(modelFile);
                    });


                //
                // Init module client
                //

                if (Options.UseEdge)
                {
                    await InitEdge();
                }

                cts = new CancellationTokenSource();
                AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
                Console.CancelKeyPress += (sender, cpe) => cts.Cancel();

                //
                // Open camera
                //

                Camera camera = null;
                if (!string.IsNullOrEmpty(Options.DeviceId))
                {
                    (var group, var device) = Camera.Select(devices, Options.DeviceId, "Color");

                    using (camera = new Camera())
                    {
                        await camera.Open(group, device, Options.Verbose);

                        //
                        // Main loop
                        //

                        do
                        {
                            //
                            // Pull image from camera
                            //

                            VideoFrame inputImage = null;
                            await BlockTimer($"Retrieving image from camera",
                                async () =>
                                {
                                    var frame = await camera.GetFrame();
                                    inputImage = frame.VideoMediaFrame.GetVideoFrame();
                                });

                            ImageFeatureValue imageTensor = ImageFeatureValue.CreateFromVideoFrame(inputImage);

                            //
                            // Evaluate model
                            //

                            ScoringOutput outcome = null;
                            await BlockTimer("Running the model",
                                async () =>
                                {
                                    var input = new ScoringInput() { data_0 = imageTensor };
                                    outcome = await __model.EvaluateAsync(input);
                                });

                            //
                            // Print results
                            //

                            var resultVector = outcome.softmaxout_1.GetAsVectorView();
                            var message = ResultsToMessage(resultVector);
                            var dataBuffer = JsonConvert.SerializeObject(message);
                            Console.WriteLine($"{DateTime.Now.ToLocalTime()} Sending: {dataBuffer}");

                            //
                            // Send results to Edge
                            //

                            if (Options.UseEdge)
                            { 
                                var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                                await ioTHubModuleClient.SendEventAsync("resultsOutput", eventMessage); 

                                // Let's not totally spam Edge :)
                                await Task.Delay(500);
                            }
                        }
                        while (Options.RunForever && ! cts.Token.IsCancellationRequested);
                    }
                }

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

        private static MessageBody ResultsToMessage(IReadOnlyList<float> resultVector)
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

            var message = new MessageBody();
            message.results = new LabelResult[3];

            for (int i = 0; i < 3; i++)
            {
                message.results[i] = new LabelResult() { label = labels[topProbabilityLabelIndexes[i]], confidence = topProbabilities[i] };
            }

            return message;
        }

        
        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        private static async Task InitEdge()
        {
            AmqpTransportSettings amqpSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { amqpSetting };

            // Open a connection to the Edge runtime
            ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine($"{DateTime.Now.ToLocalTime()} IoT Hub module client initialized.");
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
}
    }
}