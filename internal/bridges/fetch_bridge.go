package bridges

import (
	"bytes"
	"context"
	"encoding/json"
	"io"
	"net/http"
	"strings"
	"time"

	"github.com/dop251/goja"
)

type FetchOptions struct {
	Method  string            `json:"method"`
	Headers map[string]string `json:"headers"`
	Body    string            `json:"body"`
}

type FetchBridge struct {
	client *http.Client
}

func NewFetchBridge() *FetchBridge {
	return &FetchBridge{
		client: &http.Client{
			Timeout: 15 * time.Second,
		},
	}
}

func (fb *FetchBridge) Fetch(call goja.FunctionCall, vm *goja.Runtime) goja.Value {
	if len(call.Arguments) < 1 {
		panic(vm.NewTypeError("fetch requires at least 1 argument (url)"))
	}

	url := call.Arguments[0].String()
	options := FetchOptions{
		Method: "GET",
	}

	if len(call.Arguments) > 1 {
		optsVal := call.Arguments[1].Export()
		if jsonBytes, err := json.Marshal(optsVal); err == nil {
			_ = json.Unmarshal(jsonBytes, &options)
		}
	}

	var bodyReader io.Reader
	if options.Body != "" {
		bodyReader = strings.NewReader(options.Body)
	}

	req, err := http.NewRequestWithContext(context.Background(), strings.ToUpper(options.Method), url, bodyReader)
	if err != nil {
		panic(vm.NewTypeError("failed to construct HTTP request: " + err.Error()))
	}

	for k, v := range options.Headers {
		req.Header.Set(k, v)
	}

	resp, err := fb.client.Do(req)
	if err != nil {
		panic(vm.NewTypeError("network transfer layer fault execution: " + err.Error()))
	}
	defer resp.Body.Close()

	respBody, err := io.ReadAll(resp.Body)
	if err != nil {
		panic(vm.NewTypeError("failed to read network response payload body: " + err.Error()))
	}

	responseObj := vm.NewObject()
	_ = responseObj.Set("status", resp.StatusCode)
	_ = responseObj.Set("statusText", http.StatusText(resp.StatusCode))

	_ = responseObj.Set("text", func(goja.FunctionCall) goja.Value {
		return vm.ToValue(string(respBody))
	})

	_ = responseObj.Set("json", func(goja.FunctionCall) goja.Value {
		var parsedJSON any
		decoder := json.NewDecoder(bytes.NewReader(respBody))
		decoder.UseNumber()
		if err := decoder.Decode(&parsedJSON); err != nil {
			panic(vm.NewTypeError("failed to parse response text stream as valid JSON data: " + err.Error()))
		}
		return vm.ToValue(parsedJSON)
	})

	return responseObj
}
