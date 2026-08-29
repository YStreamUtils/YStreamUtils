package bridges

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"strings"
	"time"
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

func (fb *FetchBridge) Fetch(url string, options FetchOptions) (map[string]any, error) {
	method := "GET"
	if options.Method != "" {
		method = strings.ToUpper(options.Method)
	}

	var bodyReader io.Reader
	if options.Body != "" {
		bodyReader = strings.NewReader(options.Body)
	}

	req, err := http.NewRequestWithContext(context.Background(), method, url, bodyReader)
	if err != nil {
		return nil, fmt.Errorf("failed to construct HTTP request: %w", err)
	}

	for k, v := range options.Headers {
		req.Header.Set(k, v)
	}

	resp, err := fb.client.Do(req)
	if err != nil {
		return nil, fmt.Errorf("network transfer layer fault: %w", err)
	}
	defer resp.Body.Close()

	respBody, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, fmt.Errorf("failed to read response body: %w", err)
	}

	var parsedJSON interface{}
	if err := json.Unmarshal(respBody, &parsedJSON); err != nil {
		parsedJSON = string(respBody)
	}

	return map[string]any{
		"status":     resp.StatusCode,
		"statusText": http.StatusText(resp.StatusCode),
		"data":       parsedJSON,
	}, nil
}
