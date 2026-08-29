package bridges

import "github.com/dop251/goja"

type Bridge interface {
	Register(vm *goja.Runtime, hostObj *goja.Object) error
}
