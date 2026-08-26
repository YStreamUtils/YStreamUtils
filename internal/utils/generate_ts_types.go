package utils

import (
	"fmt"
	"reflect"
	"strings"
)

func GenerateTSFields(s any, overrideEventKey string) string {
	// If the user mistakenly passed a reflect.Type, unpack it.
	var t reflect.Type
	if rt, ok := s.(reflect.Type); ok {
		t = rt
	} else {
		t = reflect.TypeOf(s)
	}

	if t == nil {
		return ""
	}
	if t.Kind() == reflect.Pointer {
		t = t.Elem()
	}
	if t.Kind() != reflect.Struct {
		return ""
	}

	var sb strings.Builder
	for i := 0; i < t.NumField(); i++ {
		field := t.Field(i)

		if field.Anonymous {
			embeddedFields := GenerateTSFields(field.Type, overrideEventKey)
			sb.WriteString(embeddedFields)
			continue
		}

		jsonTag := field.Tag.Get("json")
		if jsonTag == "-" {
			continue
		}

		tags := strings.Split(jsonTag, ",")
		fieldName := tags[0]
		if fieldName == "" {
			fieldName = field.Name
		}

		var tsType string
		if field.Name == "Event" && overrideEventKey != "" {
			tsType = fmt.Sprintf(`"%s"`, overrideEventKey)
		} else if field.Name == "Platform" {
			tsType = `"twitch" | "youtube" | "kick" | string`
		} else {
			tsType = resolveTSType(field.Type)
		}
		fmt.Fprintf(&sb, "    %s: %s;\n", fieldName, tsType)
	}
	return sb.String()
}

func resolveTSType(t reflect.Type) string {
	if t.Kind() == reflect.Pointer {
		t = t.Elem()
	}
	switch t.Kind() {
	case reflect.String:
		return "string"
	case reflect.Bool:
		return "boolean"
	case reflect.Int, reflect.Int8, reflect.Int16, reflect.Int32, reflect.Int64,
		reflect.Uint, reflect.Uint8, reflect.Uint16, reflect.Uint32, reflect.Uint64,
		reflect.Float32, reflect.Float64:
		return "number"
	case reflect.Slice, reflect.Array:
		return resolveTSType(t.Elem()) + "[]"
	case reflect.Struct:
		if t.String() == "time.Time" {
			return "string"
		}
		return "{\n" + GenerateTSFields(t, "") + "    }"
	default:
		return "any"
	}
}
