package bridges

import (
	"sync"

	"github.com/dop251/goja"
)

type CacheBridge struct {
	name string
	mu   sync.RWMutex
	data map[string]any
}

func NewCacheBridge(name string) *CacheBridge {
	return &CacheBridge{
		name: name,
		data: make(map[string]any),
	}
}

func (cb *CacheBridge) Register(vm *goja.Runtime, hostObj *goja.Object) error {
	cacheObj := vm.NewObject()

	_ = cacheObj.Set("get", func(key string) any {
		cb.mu.RLock()
		defer cb.mu.RUnlock()
		return cb.data[key]
	})

	_ = cacheObj.Set("set", func(key string, val any) {
		cb.mu.Lock()
		defer cb.mu.Unlock()
		cb.data[key] = val
	})

	_ = cacheObj.Set("delete", func(key string) {
		cb.mu.Lock()
		defer cb.mu.Unlock()
		delete(cb.data, key)
	})

	_ = cacheObj.Set("clear", func() {
		cb.mu.Lock()
		defer cb.mu.Unlock()
		for k := range cb.data {
			delete(cb.data, k)
		}
	})

	return hostObj.Set("cache", cacheObj)
}
