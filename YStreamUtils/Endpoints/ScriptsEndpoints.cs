using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Models;
using YStreamUtils.Core.Services;

namespace YStreamUtils.Endpoints;

public record RegisterScriptRequest(EventKey Topic, string ScriptId, string RawJsString);
public record SaveScriptRequest(EventKey Topic, string ScriptId, string RawJsString, bool IsEnabled);
public record ScriptMetadataResponse([property: JsonPropertyName("definitions")] string Definitions);

public static class ScriptsEndpoints
{
    public static RouteGroupBuilder AddScriptsService(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost("/scripts/register", async (RegisterScriptRequest request, [FromServices] ScriptsService scriptsService) =>
            {
                await scriptsService.RegisterScriptAndBindToBusAsync(request.Topic, request.ScriptId, request.RawJsString);
                return Results.Ok();
            })
            .WithName("RegisterScriptAndBindToBus");

        groupBuilder.MapGet("/scripts/monaco-env/{topic}", (EventKey topic, [FromServices] ScriptsService scriptsService) =>
            {
                var environment = scriptsService.GetMonacoEnvironment(topic);
                return Results.Json(new ScriptMetadataResponse(environment));
            })
            .Produces<ScriptMetadataResponse>()
            .WithName("GetMonacoEnvironment");

        groupBuilder.MapGet("/scripts/{scriptId}", async (string scriptId, [FromServices] AppDbContext dbContext) =>
            {
                var script = await dbContext.UserScripts.AsNoTracking().FirstOrDefaultAsync(s => s.ScriptId == scriptId);
                return script != null ? Results.Ok(script) : Results.NotFound();
            })
            .Produces<UserScript>()
            .WithName("GetScriptById");
        
        groupBuilder.MapDelete("/scripts/{scriptId}", async (string scriptId, [FromServices] AppDbContext dbContext) =>
        {
            var rowsAffected = await dbContext.UserScripts
                .Where(s => s.ScriptId == scriptId)
                .ExecuteDeleteAsync();

            return rowsAffected == 0 ? Results.NotFound() : Results.NoContent();
        })
        .WithName("DeleteScriptById");

        groupBuilder.MapGet("/scripts", async ([FromServices] AppDbContext dbContext) =>
            {
                var scripts = await dbContext.UserScripts.AsNoTracking().ToListAsync();
                return Results.Ok(scripts);
            })
            .Produces<List<UserScript>>()
            .WithName("GetAllScripts");

        groupBuilder.MapPost("/scripts/save", async (UserScript incoming, [FromServices] AppDbContext dbContext, [FromServices] ScriptsService scriptsService) =>
            {
                var existing = await dbContext.UserScripts.FindAsync(incoming.ScriptId);
                if (existing != null)
                {
                    existing.Topic = incoming.Topic;
                    existing.RawJsString = incoming.RawJsString;
                    existing.IsEnabled = incoming.IsEnabled;
                }
                else
                {
                    await dbContext.UserScripts.AddAsync(incoming);
                }

                await dbContext.SaveChangesAsync();

                if (incoming.IsEnabled)
                {
                    await scriptsService.RegisterScriptAndBindToBusAsync(incoming.Topic, incoming.ScriptId, incoming.RawJsString);
                }

                return Results.Ok();
            })
            .WithName("SaveScript");
        
        groupBuilder.MapGet("/script/test/invoke", async ([FromServices] IEventBus bus) =>
        {
            await bus.PublishAsync(EventKey.ManualInvoke, null);
            return Results.Ok();
        })
        .WithName("InvokeScriptTest");

        return groupBuilder;
    }
}