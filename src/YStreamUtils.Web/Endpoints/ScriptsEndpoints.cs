using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Events;
using YStreamUtils.Core.Services;

namespace YStreamUtils.Web.Endpoints;

[UsedImplicitly] 
public readonly record struct RegisterScriptRequest(EventKey Topic, string ScriptId, string RawJsString);

[UsedImplicitly]
public readonly record struct ScriptMetadataResponse([property: JsonPropertyName("definitions")] string Definitions);

public static class ScriptsEndpoints
{
    public static RouteGroupBuilder AddScriptsService(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("/scripts/monaco-env/{topic}", (EventKey topic, [FromServices] ScriptsService scriptsService) =>
            {
                var environment = scriptsService.GetMonacoEnvironment(topic);
                return Results.Ok(new ScriptMetadataResponse(environment));
            });

        groupBuilder.MapGet("/scripts/{scriptId}", async (string scriptId, [FromServices] IDbContextFactory<AppDbContext> contextFactory) =>
            {
                await using var dbContext = await contextFactory.CreateDbContextAsync();
                var script = await dbContext.UserScripts.AsNoTracking().FirstOrDefaultAsync(s => s.ScriptId == scriptId);
                return script != null ? Results.Ok(script) : Results.NotFound();
            });
        
        groupBuilder.MapDelete("/scripts/{scriptId}", async (string scriptId, [FromServices] IDbContextFactory<AppDbContext> contextFactory) =>
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync();
            var rowsAffected = await dbContext.UserScripts
                .Where(s => s.ScriptId == scriptId)
                .ExecuteDeleteAsync();

            return rowsAffected == 0 ? Results.NotFound() : Results.NoContent();
        });

        groupBuilder.MapGet("/scripts", async ([FromServices] IDbContextFactory<AppDbContext> contextFactory) =>
            {
                await using var dbContext = await contextFactory.CreateDbContextAsync();
                var scripts = await dbContext.UserScripts.AsNoTracking().ToListAsync();
                return Results.Ok(scripts);
            });

        groupBuilder.MapPost("/scripts/save", async (UserScript incoming, [FromServices] IDbContextFactory<AppDbContext> contextFactory, [FromServices] ScriptsService scriptsService) =>
            {
                await using var dbContext = await contextFactory.CreateDbContextAsync();
                var existing = await dbContext.UserScripts.FindAsync(incoming.ScriptId);
                if (existing != null)
                {
                    existing.Topic = incoming.Topic;
                    existing.Source = incoming.Source;
                    existing.IsEnabled = incoming.IsEnabled;
                }
                else
                {
                    await dbContext.UserScripts.AddAsync(incoming);
                }

                await dbContext.SaveChangesAsync();

                if (incoming.IsEnabled)
                {
                    await scriptsService.RegisterScriptAndBindToBusAsync(incoming.Topic, incoming.ScriptId, incoming.Source);
                }

                return Results.Ok();
            });
        
        groupBuilder.MapGet("/script/test/invoke", async ([FromServices] IEventBus bus) =>
        {
            await bus.PublishAsync(EventKey.ManualInvoke, null);
            return Results.Ok();
        });

        return groupBuilder;
    }
}