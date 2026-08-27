package utils

import "strings"

func GetSafePluginNamespace(namespace string) string {
	return strings.ReplaceAll(namespace, "-", "_")
}
