using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.AI.MachineLearning;
using static Helpers.AsyncHelper;

namespace SqueezeNetObjectDetectionNC
{
    
    public sealed class DesktopObjectsInput
    {
        public ImageFeatureValue data; // BitmapPixelFormat: Bgra8, BitmapAlphaMode: Premultiplied, width: 227, height: 227
    }
    
    public sealed class DesktopObjectsOutput
    {
        public TensorString classLabel; // shape(-1,1)
        public IList<Dictionary<string,float>> loss;
    }
    
    public sealed class DesktopObjectsModel
    {
        private LearningModel model;
        private LearningModelSession session;
        private LearningModelBinding binding;
        public static async Task<DesktopObjectsModel> CreateFromStreamAsync(IRandomAccessStreamReference stream)
        {
            DesktopObjectsModel learningModel = new DesktopObjectsModel();
            learningModel.model = await AsAsync(LearningModel.LoadFromStreamAsync(stream));
            learningModel.session = new LearningModelSession(learningModel.model);
            learningModel.binding = new LearningModelBinding(learningModel.session);
            return learningModel;
        }
        public async Task<DesktopObjectsOutput> EvaluateAsync(DesktopObjectsInput input)
        {
            binding.Bind("data", input.data);
            var result = await AsAsync(session.EvaluateAsync(binding, "0"));
            var output = new DesktopObjectsOutput();
            output.classLabel = result.Outputs["classLabel"] as TensorString;
            output.loss = result.Outputs["loss"] as IList<Dictionary<string,float>>;
            return output;
        }
    }
}
