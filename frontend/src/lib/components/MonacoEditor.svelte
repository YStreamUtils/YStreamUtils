<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import loader from '@monaco-editor/loader';
  import * as monacoCore from 'monaco-editor';
  import { createHighlighter } from 'shiki';
  import { shikiToMonaco } from '@shikijs/monaco';

  import type { editor, IDisposable } from 'monaco-editor';
  import { getMonacoEnvironment, EventKey } from '$lib/api';
  import type { EventKey as EventKeyType } from '$lib/api';

  loader.config({ monaco: monacoCore });

  let {
    userScript = $bindable('// Try typing "eventData." or "plugins." here!\n\nhost.log("info", "Hello from Goja!");\n'),
    currentKey = EventKey.ManualInvoke
  }: Props = $props();

  interface Props {
    currentKey: EventKeyType;
    userScript: string;
  }

  let container: HTMLDivElement;

  let monacoInstance = $state.raw<typeof monacoCore>();
  let editorInstance = $state.raw<editor.IStandaloneCodeEditor>();
  let modelInstance = $state.raw<editor.ITextModel | null>(null);
  let extraLibsDisposable = $state.raw<IDisposable | null>(null);
  let isUpdatingFromModel = $state(false);

  async function updateTypings(currentKey: EventKeyType): Promise<void> {
    if (!monacoInstance) return;
    try {
      const tsDefinitions = await getMonacoEnvironment(currentKey);
      if (tsDefinitions.status != 200 || !tsDefinitions.data) {
        console.warn('[Monaco Environment] No typings found for the current event key:', currentKey);
        return;
      }

      if (extraLibsDisposable) {
        extraLibsDisposable.dispose();
      }

      const tsDefaults = monacoInstance.typescript.typescriptDefaults;
      const typeUri = 'file:///node_modules/@types/ystream-internal/index.d.ts';

      extraLibsDisposable = tsDefaults.addExtraLib(tsDefinitions.data.definitions, typeUri);
    } catch (err) {
      console.error('[Monaco Environment] Failed to inject dynamic backend payloads:', err);
    }
  }

  onMount(async () => {
    try {
      const monaco = await loader.init();
      monacoInstance = monaco as typeof monacoCore;

      const tsDefaults = monacoInstance.typescript.typescriptDefaults;
      tsDefaults?.setCompilerOptions({
        target: monacoInstance.typescript.ScriptTarget.ES2020,
        module: monacoInstance.typescript.ModuleKind.None,
        strict: true,
        allowNonTsExtensions: true,
        noLib: false
      });

      await updateTypings(currentKey);

      const fileUri = monaco.Uri.parse('file:///main.ts');
      modelInstance = monaco.editor.createModel(userScript, 'typescript', fileUri);

      const highlighter = await createHighlighter({
        themes: ['one-dark-pro'],
        langs: ['typescript', 'javascript', 'json']
      });

      shikiToMonaco(highlighter, monacoInstance as any);

      editorInstance = monacoInstance.editor.create(container, {
        model: modelInstance,
        automaticLayout: false,
        theme: 'one-dark-pro',
        minimap: { enabled: false }
      });

      editorInstance.onDidChangeModelContent(() => {
        if (isUpdatingFromModel) return;

        isUpdatingFromModel = true;
        userScript = editorInstance?.getValue() || '';
        isUpdatingFromModel = false;
      });
    } catch (error) {
      console.error('[Monaco Loader Element Injection Failed]:', error);
    }
  });

  $effect(() => {
    if (monacoInstance && currentKey) {
      updateTypings(currentKey);
    }
  });

  $effect(() => {
    if (editorInstance && userScript !== editorInstance.getValue() && !isUpdatingFromModel) {
      isUpdatingFromModel = true;
      editorInstance.setValue(userScript);
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
      container.innerHTML = '';
    }
  });

  function handleResize(event: UIEvent & { currentTarget: EventTarget & Window }) {
    editorInstance?.layout();
  }
</script>

<svelte:window onresize={handleResize} />

<div class="editor-outer-wrapper">
  <div class="editor-viewport" bind:this={container}></div>
</div>

<style>
  .editor-outer-wrapper {
    flex-grow: 1;
    width: 100%;
    min-width: 0;
    min-height: 0;
    padding-top: 1rem;
  }

  .editor-viewport {
    display: flex;
    flex-wrap: wrap;
    width: 100%;
    height: calc(100% - 1px);
    border-radius: 4px;
  }
</style>
