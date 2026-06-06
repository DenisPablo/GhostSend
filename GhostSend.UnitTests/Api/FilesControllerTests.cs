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

    private async Task<(Guid FileId, string DeleteToken)> UploadFileAsync(byte[] data, string fileName, int? maxDownloads = null, string? lifeTime = null)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(data);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "File", fileName);
        if (maxDownloads.HasValue)
            content.Add(new StringContent(maxDownloads.Value.ToString()), "MaxDownloads");
        if (lifeTime != null)
            content.Add(new StringContent(lifeTime), "LifeTime");

        var response = await _client.PostAsync("/api/v1/Files/upload", content);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UploadFileResponse>();
        return (result!.FileId, result.DeleteToken);
    }

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

    [Fact]
    public async Task DownloadFile_ShouldSucceed_WhenFileIsValid()
    {
        var (fileId, _) = await UploadFileAsync(new byte[512], "download_test.txt", 5, "01:00:00");

        var response = await _client.GetAsync($"/api/v1/Files/GetFile?Id={fileId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(512, (await response.Content.ReadAsByteArrayAsync()).Length);
    }

    [Fact]
    public async Task GetMetadata_WithNonExistentId_Returns_NotFound()
    {
        var response = await _client.GetAsync($"/api/v1/Files/GetMetadata?Id={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DownloadFile_WithNonExistentId_Returns_NotFound()
    {
        var response = await _client.GetAsync($"/api/v1/Files/GetFile?Id={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteFile_WithInvalidToken_Returns_BadRequest()
    {
        var (fileId, _) = await UploadFileAsync(new byte[128], "delete_invalid.txt", 5, "01:00:00");

        var response = await _client.DeleteAsync($"/api/v1/Files/Delete?Id={fileId}&DeleteToken=invalidtoken123");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteFile_WithNonExistentId_Returns_NotFound()
    {
        var response = await _client.DeleteAsync($"/api/v1/Files/Delete?Id={Guid.NewGuid()}&DeleteToken=sometoken");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

