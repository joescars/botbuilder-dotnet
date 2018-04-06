// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Cognitive.LUIS;

//[assembly: InternalsVisibleTo("Microsoft.Bot.Builder.LUIS.Tests")]
namespace Microsoft.Bot.Builder.LUIS
{
    /// <summary>
    /// A Middleware for running the Luis recognizer
    /// This could eventually be generalized and moved to the core Bot Builder library
    /// in order to support multiple recognizers
    /// </summary>
    public class LuisRecognizerMiddleware : IMiddleware
    {
        public const string LuisRecognizerResultKey = "LuisRecognizerResult";
        public const string LuisTraceEventName = "https://www.luis.ai/schemas/trace";
        public const string Obfuscated = "****";
        private readonly ILuisRecognizer _luisRecognizer;
        private readonly ILuisModel _luisModel;
        private readonly ILuisOptions _luisOptions;

        public LuisRecognizerMiddleware(ILuisModel luisModel, ILuisRecognizerOptions luisRecognizerOptions = null, ILuisOptions luisOptions = null)
        {
            _luisModel = luisModel ?? throw new ArgumentNullException(nameof(luisModel));
            _luisOptions = luisOptions;
            _luisRecognizer = new LuisRecognizer(luisModel, luisRecognizerOptions, luisOptions);
        }

        public async Task OnProcessRequest(ITurnContext context, MiddlewareSet.NextDelegate next)
        {
            BotAssert.ContextNotNull(context);

            if (context.Activity.Type == ActivityTypes.Message)
            {
                var utterance = context.Activity.AsMessageActivity().Text;
                var result = await _luisRecognizer.CallAndRecognize(utterance, CancellationToken.None).ConfigureAwait(false);
                context.Services.Add(LuisRecognizerResultKey, result.recognizerResult);

                var traceActivity = Activity.CreateEventActivity();
                traceActivity.Name = LuisTraceEventName;
                traceActivity.Value = new LuisTraceInfo
                {
                    RecognizerResult = result.recognizerResult,
                    LuisModel = RemoveSensitiveData(_luisModel),
                    LuisOptions = _luisOptions,
                    LuisResult = result.luisResult
                };
                await context.SendActivity(traceActivity).ConfigureAwait(false);
            }
            await next().ConfigureAwait(false);
        }

        internal static ILuisModel RemoveSensitiveData(ILuisModel luisModel)
        {
            return new LuisModel(luisModel.ModelID, Obfuscated, luisModel.UriBase, luisModel.ApiVersion);
        }
    }
}