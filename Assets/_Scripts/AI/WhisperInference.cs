using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Sentis;
using UnityEngine;

public class WhisperInference : IDisposable
{
    Worker decoder1, decoder2, encoder, spectrogram;
    Worker argmax;

    const int maxTokens = 100;
    const int END_OF_TEXT = 50257;
    const int START_OF_TRANSCRIPT = 50258;
    const int KOREAN = 50264;
    const int TRANSCRIBE = 50359;
    const int NO_TIME_STAMPS = 50363;

    string[] tokens;
    NativeArray<int> outputTokens;
    NativeArray<int> lastToken;
    Tensor<int> lastTokenTensor;
    Tensor<int> tokensTensor;
    Tensor<float> encodedAudio;
    Tensor<float> currentAudioChunkInput;

    private int[] whiteSpaceCharacters = new int[256];

    public WhisperInference(Model decoder1Model, Model decoder2Model, Model encoderModel, Model spectrogramModel, TextAsset vocabAsset)
    {
        SetupWhiteSpaceShifts();
        GetTokens(vocabAsset);

        decoder1 = new Worker(decoder1Model, BackendType.GPUCompute);
        decoder2 = new Worker(decoder2Model, BackendType.GPUCompute);

        FunctionalGraph graph = new FunctionalGraph();
        var input = graph.AddInput(DataType.Float, new DynamicTensorShape(1, 1, 51865));
        var amax = Functional.ArgMax(input, -1, false);
        var selectTokenModel = graph.Compile(amax);
        argmax = new Worker(selectTokenModel, BackendType.GPUCompute);

        encoder = new Worker(encoderModel, BackendType.GPUCompute);
        spectrogram = new Worker(spectrogramModel, BackendType.GPUCompute);

        outputTokens = new NativeArray<int>(maxTokens, Allocator.Persistent);
        lastToken = new NativeArray<int>(1, Allocator.Persistent);
        lastTokenTensor = new Tensor<int>(new TensorShape(1, 1), new[] { NO_TIME_STAMPS });

        tokensTensor = new Tensor<int>(new TensorShape(1, maxTokens));
        ComputeTensorData.Pin(tokensTensor);
    }

    public async Awaitable<string> TranscribeAudioChunk(float[] rawChunkData)
    {
        if (currentAudioChunkInput == null)
        {
            currentAudioChunkInput = new Tensor<float>(new TensorShape(1, rawChunkData.Length), rawChunkData);
        }
        else
        {
            currentAudioChunkInput.Upload(rawChunkData);
        }

        EncodeAudio();

        outputTokens[0] = START_OF_TRANSCRIPT;
        outputTokens[1] = KOREAN;
        outputTokens[2] = TRANSCRIBE;
        int tokenCount = 3;
        lastToken[0] = NO_TIME_STAMPS;

        tokensTensor.Reshape(new TensorShape(1, tokenCount));
        tokensTensor.dataOnBackend.Upload<int>(outputTokens, tokenCount);
        lastTokenTensor.dataOnBackend.Upload<int>(lastToken, 1);

        string currentChunkString = "";
        bool transcribe = true;

        while (transcribe && tokenCount < (outputTokens.Length - 1))
        {
            var result = await InferenceStepInternal(tokenCount, transcribe, currentChunkString);
            tokenCount = result.NewTokenCount;
            transcribe = result.NewTranscribeState;
            currentChunkString = result.NewChunkString;

            if (!transcribe)
            {
                break;
            }
        }
        return currentChunkString;
    }

    void EncodeAudio()
    {
        spectrogram.Schedule(currentAudioChunkInput);
        var logmel = spectrogram.PeekOutput() as Tensor<float>;
        encoder.Schedule(logmel);
        encodedAudio = encoder.PeekOutput() as Tensor<float>;
    }

    async Awaitable<(int NewTokenCount, bool NewTranscribeState, string NewChunkString)> InferenceStepInternal(int tokenCount, bool transcribe, string currentChunkString)
    {
        decoder1.SetInput("input_ids", tokensTensor);
        decoder1.SetInput("encoder_hidden_states", encodedAudio);
        decoder1.Schedule();

        var past_key_values_0_decoder_key = decoder1.PeekOutput("present.0.decoder.key") as Tensor<float>;
        var past_key_values_0_decoder_value = decoder1.PeekOutput("present.0.decoder.value") as Tensor<float>;
        var past_key_values_1_decoder_key = decoder1.PeekOutput("present.1.decoder.key") as Tensor<float>;
        var past_key_values_1_decoder_value = decoder1.PeekOutput("present.1.decoder.value") as Tensor<float>;
        var past_key_values_2_decoder_key = decoder1.PeekOutput("present.2.decoder.key") as Tensor<float>;
        var past_key_values_2_decoder_value = decoder1.PeekOutput("present.2.decoder.value") as Tensor<float>;
        var past_key_values_3_decoder_key = decoder1.PeekOutput("present.3.decoder.key") as Tensor<float>;
        var past_key_values_3_decoder_value = decoder1.PeekOutput("present.3.decoder.value") as Tensor<float>;

        var past_key_values_0_encoder_key = decoder1.PeekOutput("present.0.encoder.key") as Tensor<float>;
        var past_key_values_0_encoder_value = decoder1.PeekOutput("present.0.encoder.value") as Tensor<float>;
        var past_key_values_1_encoder_key = decoder1.PeekOutput("present.1.encoder.key") as Tensor<float>;
        var past_key_values_1_encoder_value = decoder1.PeekOutput("present.1.encoder.value") as Tensor<float>;
        var past_key_values_2_encoder_key = decoder1.PeekOutput("present.2.encoder.key") as Tensor<float>;
        var past_key_values_2_encoder_value = decoder1.PeekOutput("present.2.encoder.value") as Tensor<float>;
        var past_key_values_3_encoder_key = decoder1.PeekOutput("present.3.encoder.key") as Tensor<float>;
        var past_key_values_3_encoder_value = decoder1.PeekOutput("present.3.encoder.value") as Tensor<float>;


        decoder2.SetInput("input_ids", lastTokenTensor);
        decoder2.SetInput("past_key_values.0.decoder.key", past_key_values_0_decoder_key);
        decoder2.SetInput("past_key_values.0.decoder.value", past_key_values_0_decoder_value);
        decoder2.SetInput("past_key_values.1.decoder.key", past_key_values_1_decoder_key);
        decoder2.SetInput("past_key_values.1.decoder.value", past_key_values_1_decoder_value);
        decoder2.SetInput("past_key_values.2.decoder.key", past_key_values_2_decoder_key);
        decoder2.SetInput("past_key_values.2.decoder.value", past_key_values_2_decoder_value);
        decoder2.SetInput("past_key_values.3.decoder.key", past_key_values_3_decoder_key);
        decoder2.SetInput("past_key_values.3.decoder.value", past_key_values_3_decoder_value);

        decoder2.SetInput("past_key_values.0.encoder.key", past_key_values_0_encoder_key);
        decoder2.SetInput("past_key_values.0.encoder.value", past_key_values_0_encoder_value);
        decoder2.SetInput("past_key_values.1.encoder.key", past_key_values_1_encoder_key);
        decoder2.SetInput("past_key_values.1.encoder.value", past_key_values_1_encoder_value);
        decoder2.SetInput("past_key_values.2.encoder.key", past_key_values_2_encoder_key);
        decoder2.SetInput("past_key_values.2.encoder.value", past_key_values_2_encoder_value);
        decoder2.SetInput("past_key_values.3.encoder.key", past_key_values_3_encoder_key);
        decoder2.SetInput("past_key_values.3.encoder.value", past_key_values_3_encoder_value);

        decoder2.Schedule();

        var logits = decoder2.PeekOutput("logits") as Tensor<float>;
        argmax.Schedule(logits);
        using var t_Token = await argmax.PeekOutput().ReadbackAndCloneAsync() as Tensor<int>;
        int index = t_Token[0];

        if (tokenCount < maxTokens)
        {
            outputTokens[tokenCount] = lastToken[0];
        }
        else
        {
            UnityEngine.Debug.LogWarning("maxTokens 제한에 도달했습니다. 텍스트 생성이 중단될 수 있습니다.");
            transcribe = false;
            return (tokenCount, transcribe, currentChunkString);
        }

        lastToken[0] = index;
        tokenCount++;

        tokensTensor.Reshape(new TensorShape(1, tokenCount));
        tokensTensor.dataOnBackend.Upload<int>(outputTokens, tokenCount);
        lastTokenTensor.dataOnBackend.Upload<int>(lastToken, 1);

        if (index == END_OF_TEXT)
        {
            transcribe = false;
        }
        else if (index >= 0 && index < tokens.Length)
        {
            currentChunkString += GetUnicodeText(tokens[index]);
        }
        else
        {
            currentChunkString += "";
            UnityEngine.Debug.LogWarning($"모델이 유효하지 않은 토큰 인덱스({index})를 반환했습니다. 빈 문자열로 처리합니다.");
        }

        return (tokenCount, transcribe, currentChunkString);
    }

    void GetTokens(TextAsset vocabAsset)
    {
        var vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(vocabAsset.text);
        tokens = new string[vocab.Count];
        foreach (var item in vocab)
        {
            tokens[item.Value] = item.Key;
        }
    }

    string GetUnicodeText(string text)
    {
        try
        {
            var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"유니코드 변환 중 오류 발생: {ex.Message}. 원본 텍스트: '{text}'");
            return "";
        }
    }

    string ShiftCharacterDown(string text)
    {
        string outText = "";
        foreach (char letter in text)
        {
            outText += ((int)letter <= 256) ? letter : (char)whiteSpaceCharacters[(int)(letter - 256)];
        }
        return outText;
    }

    void SetupWhiteSpaceShifts()
    {
        for (int i = 0, n = 0; i < 256; i++)
        {
            if (IsWhiteSpace((char)i)) whiteSpaceCharacters[n++] = i;
        }
    }

    bool IsWhiteSpace(char c)
    {
        return !(('!' <= c && c <= '~') || ('\uFF01' <= c && c <= '\uFF5E') || ('\u3000' <= c && c <= '\u303F'));
    }

    public void Dispose()
    {
        decoder1?.Dispose();
        decoder2?.Dispose();
        encoder?.Dispose();
        spectrogram?.Dispose();
        argmax?.Dispose();
        currentAudioChunkInput?.Dispose();
        lastTokenTensor?.Dispose();
        tokensTensor?.Dispose();
        outputTokens.Dispose();
        lastToken.Dispose();
    }
}