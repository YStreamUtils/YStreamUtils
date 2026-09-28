<script lang="ts">
    import { onDestroy, onMount } from 'svelte';
    import { EditorView, basicSetup } from 'codemirror';
    import { javascript } from '@codemirror/lang-javascript';
    import { oneDark } from '@codemirror/theme-one-dark';
    import { Compartment, EditorState } from '@codemirror/state';
    import { acceptCompletion, autocompletion } from '@codemirror/autocomplete';
    import { indentMore, indentLess } from '@codemirror/commands';
    import { keymap } from '@codemirror/view';

    import type { EventKey as EventKeyType } from '$lib/api';
    import { EventKey, getMonacoEnvironment } from '$lib/api';

    let {
        userScript = $bindable('// Type "plugins." or "context." here!n'),
        currentKey = EventKey["app:manual"]
    }: Props = $props();

    interface Props {
        currentKey: EventKeyType;
        userScript: string;
    }

    let container: HTMLDivElement;
    let viewInstance = $state.raw<EditorView | null>(null);
    let isUpdatingFromState = false;

    const autocompleteCompartment = new Compartment();

    let masterGlobals = $state.raw<any>({});
    let masterTypes = $state.raw<any>({});

    async function updateSchema(currentKey: EventKeyType): Promise<void> {
        if (!viewInstance) return;
        try {
            const res = await getMonacoEnvironment(currentKey);
            if (res.status !== 200 || !res.data) return;

            const rawJsonPayload = JSON.parse(res.data.definitions);
            const { globals, types } = rawJsonPayload;

            masterGlobals = { ...globals };
            masterTypes = { ...types };

            const jintCompleter = autocompletion({
                override: [
                    async (ctx) => {
                        const before = ctx.matchBefore(/[a-zA-Z0-9_.]*/);
                        if (!before || (before.from === before.to && !ctx.explicit)) return null;

                        const parts = before.text.split('.');
                        const currentTypedWord = parts[parts.length - 1];

                        let currentScope: any = masterGlobals;

                        for (let i = 0; i < parts.length - 1; i++) {
                            const part = parts[i];
                            if (!currentScope) return null;

                            let nextNode = currentScope[part];

                            if (!nextNode && currentScope[`${part}()`]) {
                                nextNode = currentScope[`${part}()`]?.returns;
                            }

                            if (nextNode && typeof nextNode === 'object' && nextNode.inlineMembers) {
                                currentScope = nextNode.inlineMembers;
                            } else if (nextNode && typeof nextNode === 'object' && nextNode.referenceType) {
                                currentScope = masterTypes[nextNode.referenceType];
                            } else if (nextNode && typeof nextNode === 'object' && nextNode.arrayOfType) {
                                currentScope = nextNode.arrayOfType.referenceType ? masterTypes[nextNode.arrayOfType.referenceType] : null;
                            } else {
                                currentScope = nextNode;
                            }
                        }

                        if (!currentScope || typeof currentScope !== 'object') return null;

                        const options = Object.keys(currentScope).map((key) => {
                            const value = currentScope[key];
                            const isCleanMethod = key.endsWith('()') || (value && value.isMethod);
                            const displayLabel = key.endsWith('()') ? key.slice(0, -2) : key;

                            let detailStr = 'Property';
                            if (isCleanMethod) {
                                detailStr = value.detail || '()';
                            } else if (value && value.referenceType) {
                                detailStr = value.referenceType;
                            } else if (typeof value === 'string') {
                                detailStr = value;
                            }

                            return {
                                label: displayLabel,
                                type: isCleanMethod ? 'method' : 'property',
                                detail: detailStr
                            };
                        });

                        return {
                            from: before.from + before.text.lastIndexOf('.') + 1,
                            options: options.filter(o => o.label.startsWith(currentTypedWord))
                        };
                    }
                ]
            });

            viewInstance.dispatch({
                effects: autocompleteCompartment.reconfigure(jintCompleter)
            });
        } catch (err) {
            console.error('[CodeMirror JSON Parsing Engine] Schema Sync failure:', err);
        }
    }

    onMount(() => {
        const tabInterceptor = keymap.of([
            {
                key: 'Tab',
                run: (targetView) => {
                    if (acceptCompletion(targetView)) {
                        return true;
                    }

                    return indentMore(targetView);
                }
            },
            {
                key: 'Shift-Tab',
                run: (targetView) => {
                    return indentLess(targetView);
                }
            }
        ]);
        
        
        viewInstance = new EditorView({
            parent: container,
            state: EditorState.create({
                doc: userScript,
                extensions: [
                    tabInterceptor,
                    basicSetup,
                    oneDark,
                    javascript(),
                    autocompleteCompartment.of([]),
                    EditorView.updateListener.of((u) => {
                        if (u.docChanged && !isUpdatingFromState) {
                            isUpdatingFromState = true;
                            userScript = u.state.doc.toString();
                            isUpdatingFromState = false;
                        }
                    })
                ]
            })
        });
        updateSchema(currentKey);
    });

	$effect(() => { if (viewInstance && currentKey) updateSchema(currentKey); });

	$effect(() => {
        if (viewInstance && userScript !== viewInstance.state.doc.toString() && !isUpdatingFromState) {
            isUpdatingFromState = true;
            viewInstance.dispatch({
                changes: { from: 0, to: viewInstance.state.doc.length, insert: userScript }
            });
            isUpdatingFromState = false;
        }
    });

    onDestroy(() => viewInstance?.destroy());
</script>

<div class="editor-viewport" bind:this={container}></div>

<style>
    :global(.cm-editor) { height: 100% !important; }
    .editor-viewport { width: 100%; height: 500px; border-radius: 4px; overflow: hidden; }
</style>
