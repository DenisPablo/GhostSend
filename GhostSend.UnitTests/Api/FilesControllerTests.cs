//TODO: Test de descargas correcto

//TODO: Test de subidas incorrecto Por Tamaño
//TODO: Test de descargas incorrecto Por ID
//TODO: Test de eliminacion incorrecto Por ID
//TODO: Test de Metadatos incorrecto Por ID
using System.Net;
using System.Net.Http.Json;
using GhostSend.Api.DTOs.Responses;
using GhostSend.Application.Common.Settings;
using GhostSend.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GhostSend.UnitTests.Api;

public class FilesControllerTests(IntegrationTestWebAppFactory factory) : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();


    [Fact]
    public async Task UploadFile_And_DeleteFile_And_GetMetadata_Works_Correctly()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[1024]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "File", "test.txt");
        content.Add(new StringContent("5"), "MaxDownloads");
        content.Add(new StringContent("00:10:00"), "LifeTime");

        var uploadFile = await _client.PostAsync("/api/v1/Files/upload", content);

        Assert.Equal(HttpStatusCode.Created, uploadFile.StatusCode);
        var uploadResult = await uploadFile.Content.ReadFromJsonAsync<UploadFileResponse>();

        Guid fileId = uploadResult!.FileId;

        var deleteToken = uploadResult.DeleteToken;

        var metadataResponse = await _client.GetAsync($"/api/v1/Files/GetMetadata?Id={fileId}");

        Assert.Equal(HttpStatusCode.OK, metadataResponse.StatusCode);
        var metadata = await metadataResponse.Content.ReadFromJsonAsync<FileMetadataResponse>();

        Assert.Equal(fileId, metadata!.FileId);
        Assert.Equal("test.txt", metadata.FileName);
        Assert.Equal(1024, metadata.FileSize);
        Assert.Equal(5, metadata.MaxDownloads);
        Assert.Equal("00:10:00", metadata.LifeTime);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/Files/Delete?Id={fileId}&DeleteToken={deleteToken}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task UploadFile_With_Invalid_MaxDownloads_Returns_BadRequest()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[1024]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "File", "test.txt");
        content.Add(new StringContent("invalid"), "MaxDownloads");
        content.Add(new StringContent("00:10:00"), "LifeTime");

        var uploadFile = await _client.PostAsync("/api/v1/Files/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, uploadFile.StatusCode);
    }

    [Fact]
    public async Task UploadFile_Exceeds_MaxFileSize_Returns_BadRequest()
    {
        var fileSettings = factory.Services.GetRequiredService<IOptions<FileSettings>>().Value;
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[fileSettings.MaxFileSizeInBytes + 1]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "File", "test.txt");
        content.Add(new StringContent("5"), "MaxDownloads");
        content.Add(new StringContent("00:10:00"), "LifeTime");

        var uploadFile = await _client.PostAsync("/api/v1/Files/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, uploadFile.StatusCode);
    }
}

