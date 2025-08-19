using System;
using System.Linq;
using System.Text;

public static class GraphUtils {
    public static string FieldToReadableName(this string fieldName) {
        if (string.IsNullOrEmpty(fieldName))
            return fieldName;

        if (fieldName.All(c => Char.IsUpper(c) || !Char.IsLetter(c)))
            return fieldName;

        if (fieldName.Length == 1)
            return new StringBuilder().Append(char.ToUpper(fieldName[0])).ToString();

        var sb = new StringBuilder();
        int added = 0;

        if (fieldName[0] != '_' && (fieldName.Length <= 2 || fieldName[1] != '_')) {
            sb.Append(char.ToUpper(fieldName[0]));
            added++;
        }

        for (int i = 1; i < fieldName.Length; i++) {
            if ((i > 1 || added > 0) && char.IsUpper(fieldName[i])) {
                sb.Append(' ');
                added++;
            } else if (fieldName[i] == '_')
                continue;

            sb.Append(added == 0 ? char.ToUpper(fieldName[i]) : fieldName[i]);
            added++;
        }

        return sb.ToString();
    }
}