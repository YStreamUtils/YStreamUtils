package bridges

import (
	"fmt"
	"slices"

	"github.com/YStreamUtils/YStreamUtils-Plugin-Registry/ci/types"
	"github.com/dop251/goja"
	"github.com/ystreamutils/YStreamUtils/internal/models"
	"github.com/ystreamutils/YStreamUtils/internal/ports"
)

type AuthBridge struct {
	vault ports.SecretVault
}

func NewAuthBridge(vault ports.SecretVault) *AuthBridge {
	return &AuthBridge{
		vault: vault,
	}
}

func (ab *AuthBridge) GetAccessToken(call goja.FunctionCall, vm *goja.Runtime, pluginName string, permissions []types.Permission) goja.Value {
	if len(call.Arguments) < 1 {
		panic(vm.NewTypeError("getAccessToken requires at least 1 argument (platformName)"))
	}

	platformName := call.Arguments[0].String()
	requiredPerm := types.Permission(fmt.Sprintf("auth:%s", platformName))

	hasPerm := slices.Contains(permissions, requiredPerm)

	if !hasPerm {
		panic(vm.NewTypeError(fmt.Sprintf("Security Error: Plugin '%s' lacks '%s' permission.", pluginName, requiredPerm)))
	}

	if ab.vault == nil {
		panic(vm.NewTypeError("Secret vault service layer is uninitialized on the host context"))
	}

	platform := models.Platform(platformName)
	token, err := ab.vault.GetValidSession(platform)
	if err != nil {
		panic(vm.NewTypeError(fmt.Sprintf("Authentication token retrieval fault for platform '%s': %s", platformName, err.Error())))
	}

	if token == nil || token.AccessToken == "" {
		panic(vm.NewTypeError(fmt.Sprintf("Authentication error: No active or valid session found for platform '%s'", platformName)))
	}

	return vm.ToValue(token.AccessToken)
}
