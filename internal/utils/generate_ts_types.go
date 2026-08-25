package utils

import (
	"fmt"
	"reflect"
	"strings"
)

func GenerateTSFields(s interface{}, overrideEventKey string) string {
	t := reflect.TypeOf(s)
	if t.Kind() == reflect.Ptr {
		t = t.Elem()
	}
	if t.Kind() != reflect.Struct {
		return ""
	}

	var sb strings.Builder
	for i := 0; i < t.NumField(); i++ {
		field := t.Field(i)

		if field.Anonymous {
			// Forward the override key recursively
			embeddedFields := GenerateTSFields(reflect.New(field.Type).Elem().Interface(), overrideEventKey)
			sb.WriteString(embeddedFields)
		}

		jsonTag := field.Tag.Get("json")
		if jsonTag == "-" || field.Anonymous {
			continue
		}

		tags := strings.Split(jsonTag, ",")
		fieldName := tags[0]
		if fieldName == "" {
			fieldName = field.Name
		}

		var tsType string
		// FIXED: Explicitly force the string literal type using our direct string token parameter
		if field.Name == "Event" && overrideEventKey != "" {
			tsType = fmt.Sprintf(`"%s"`, overrideEventKey)
		} else if field.Name == "Platform" {
			tsType = `"twitch" | "youtube" | "kick" | string`
		} else {
			// Pass a blank token down to nested sub-structures so they don't overwrite child strings
			tsType = resolveTSType(field.Type)
		}
		sb.WriteString(fmt.Sprintf("    %s: %s;\n", fieldName, tsType))
	}
	return sb.String()
}

func resolveTSType(t reflect.Type) string {
	if t.Kind() == reflect.Ptr {
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
		// Pass an empty override parameter to inner structs
		return "{\n" + GenerateTSFields(reflect.New(t).Elem().Interface(), "") + "    }"
	default:
		return "any"
	}
}
