using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using OllamaApp.Shared;
using OllamaSharp;
using static System.Net.Mime.MediaTypeNames;

namespace OllamaApp.Client.Pages;

public partial class Ollama
{
    private readonly IJSRuntime _jSRuntime;
    public Ollama(IJSRuntime jSRuntime)
    {
        _jSRuntime = jSRuntime;
    }
    public List<MessageItem> Messages = new();

    public bool clipboard { get; set; } = false;
    public bool resend { get; set; } = false;
    public string? Input { get; set; }
    public string? SelectedModel { get; set; }
    public IEnumerable<OllamaSharp.Models.Model> Models { get; set; } = new List<OllamaSharp.Models.Model>();
    public bool IsBussy { get; set; }
    public string? Message { get; set; }
    public bool IsStreming { get; set; } = false;
    public string ConnectionString { get; set; } = "http://localhost:11434";
    public OllamaApiClient? OllamaApiClient { get; set; }
    public Chat? OllamaChatClient { get; set; }
    public bool? Status { get; set; } = false;
    public string? ExceptionMessage { get; set; }
    private bool IsValid => OllamaChatClient != null
        && OllamaApiClient != null
        && !string.IsNullOrEmpty(SelectedModel)
        && !string.IsNullOrEmpty(Input)
        && !IsBussy
        && Status == true;
    protected override async Task OnInitializedAsync()
    {
        await ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        try
        {

            IsBussy = true;
            ExceptionMessage = string.Empty;
            OllamaApiClient = new OllamaApiClient(ConnectionString);
            OllamaChatClient = new Chat(OllamaApiClient);
            Models = await OllamaApiClient.ListLocalModelsAsync();
            SelectedModel = Models.FirstOrDefault()?.Name!;
            Status = true;
            IsBussy = false;

        }
        catch (Exception ex)
        {
            Models = new List<OllamaSharp.Models.Model>();
            SelectedModel = null;
            Status = false;
            ExceptionMessage = ex.Message;

        }
    }
    private async Task CopyTextToClipboard(string text)
    {
        clipboard = true;
        await _jSRuntime.InvokeVoidAsync("clipboardCopy.copyText", text);
        await Task.Delay(1000);
        clipboard = false;
    }
    private async Task Resend(string? text)
    {
        resend = true;
        await CallAsync(text);
        await Task.Delay(1000);
        resend = false;

    }
    private async Task CallAsync(string input)
    {
        

        try
        {
            ExceptionMessage = string.Empty;
            IsBussy = true;



            OllamaApiClient!.SelectedModel = SelectedModel;

            var guid = Guid.NewGuid();
            var request = new MessageItem(input);
            Messages.Add(request);

            var response = new MessageItem(string.Empty, true, request);
            Messages.Add(response);
            Input = string.Empty;
            if (IsStreming)
            {
                await foreach (var stream in OllamaChatClient!.SendAsync(request.Message))
                {
                    response.Message = response.Message + stream;
                    await Task.Delay(1);
                    StateHasChanged();
                    await _jSRuntime.InvokeVoidAsync("scrollToBottom", "scrollable_div");

                }
            }
            else
            {
                response.IsLoading = true;
                var task = await OllamaApiClient.CompleteAsync(request.Message);
                response.IsLoading = false;
                response.Message = task.Message.Text;
                StateHasChanged();
                await _jSRuntime.InvokeVoidAsync("scrollToBottom", "scrollable_div");

            }
        }
        catch (Exception ex)
        {
            ExceptionMessage = ex.Message;

        }
        finally
        {
            IsBussy = false;


        }


    }
}