<script lang="ts">
  import { onMount, onDestroy } from "svelte";
  import loader from "@monaco-editor/loader";
  import * as monacoCore from "monaco-editor";
  import { createHighlighter } from "shiki";
  import { shikiToMonaco } from "@shikijs/monaco";

  import type { editor, IDisposable } from "monaco-editor";
  import { GetMonacoEnvironment } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/services/scriptsservice";

  loader.config({ monaco: monacoCore });

  let {
    value = $bindable(""),
    eventKey = "stream:chat_message",
  }: {
    value: string;
    eventKey: string;
  } = $props();

  let container: HTMLDivElement;

  let monacoInstance = $state.raw<typeof monacoCore>();
  let editorInstance = $state.raw<editor.IStandaloneCodeEditor | null>(null);
  let modelInstance = $state.raw<editor.ITextModel | null>(null);
  let extraLibsDisposable = $state.raw<IDisposable | null>(null);
  let isUpdatingFromModel = $state(false);

  async function updateTypings(currentKey: string): Promise<void> {
    if (!monacoInstance) return;
    try {
      const tsDefinitions = await GetMonacoEnvironment(currentKey);

      if (extraLibsDisposable) {
        extraLibsDisposable.dispose();
      }

      const typeContent = `
        declare global {
          ${tsDefinitions}
        }
        export {};
      `;

      const tsDefaults = monacoInstance.typescript.typescriptDefaults;
      const typeUri = "file:///node_modules/@types/ystream-internal/index.d.ts";

      extraLibsDisposable = tsDefaults.addExtraLib(typeContent, typeUri);
    } catch (err) {
      console.error(
        "[Monaco Environment] Failed to inject dynamic backend payloads:",
        err,
      );
    }
  }

  onMount(async () => {
    try {
      const monaco = await loader.init();
      monacoInstance = monaco as typeof monacoCore;

      const tsDefaults = monacoInstance?.typescript.typescriptDefaults;
      tsDefaults?.setCompilerOptions({
        target: monacoInstance.typescript.ScriptTarget.ES2020,
        module: monacoInstance.typescript.ModuleKind.None,
        strict: true,
        allowNonTsExtensions: true,
      });

      await updateTypings(eventKey);

      const fileUri = monaco.Uri.parse("file:///main.ts");
      modelInstance = monaco.editor.createModel(value, "typescript", fileUri);

      const highlighter = await createHighlighter({
        themes: ["one-dark-pro"],
        langs: ["typescript", "javascript", "json"],
      });

      shikiToMonaco(highlighter, monaco);

      editorInstance = monaco.editor.create(container, {
        model: modelInstance,
        automaticLayout: true,
        theme: "one-dark-pro",
        minimap: { enabled: false },
      });

      editorInstance?.onDidChangeModelContent(() => {
        if (isUpdatingFromModel) return;

        isUpdatingFromModel = true;
        value = editorInstance?.getValue() || "";
        isUpdatingFromModel = false;
      });
    } catch (error) {
      console.error("[Monaco Loader Element Injection Failed]:", error);
    }
  });

  $effect(() => {
    if (monacoInstance && eventKey) {
      updateTypings(eventKey);
    }
  });

  $effect(() => {
    if (
      editorInstance &&
      value !== editorInstance.getValue() &&
      !isUpdatingFromModel
    ) {
      isUpdatingFromModel = true;
      editorInstance.setValue(value);
      isUpdatingFromModel = false;
    }
  });

  onDestroy(() => {
    if (extraLibsDisposable) {
      extraLibsDisposable.dispose();
    }
    if (modelInstance) {
      modelInstance.dispose();
    }
    if (editorInstance) {
      editorInstance.dispose();
    }
    if (container) {
      container.innerHTML = "";
    }
  });
</script>

<div class="editor-viewport" bind:this={container}></div>

<style>
  .editor-viewport {
    display: flex;
    flex-wrap: wrap;
    width: 100%;
    height: 400px;
    min-height: 200px;
    overflow: hidden;
    border-radius: 4px;
  }
</style>
