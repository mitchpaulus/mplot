using System.Text;

namespace MplotLib;

public class CsvReader
{
    private readonly StringBuilder _b = new(1000);

    public List<string> ParseCsvLine(string inputLine)
    {
        _b.Clear();
        if (inputLine.Length > _b.Capacity) _b.Capacity = inputLine.Length;
        List<string> fields = new();

        int index = 0;
        int length = inputLine.Length;

        while (index < length)
        {
            if (inputLine[index] == '"')
            {
                index++;
                // Parse escaped
                while (index < length)
                {
                    if (inputLine[index] != '"')
                    {
                        _b.Append(inputLine[index]);
                        index++;
                    }
                    else
                    {
                        if (index >= length) throw new InvalidDataException("Unfinished quoted field");

                        if (index + 1 < length)
                        {
                            // Check whether next item is quote
                            if (inputLine[index + 1] == '"')
                            {
                                _b.Append('"');
                                index += 2;
                            }
                            else if (inputLine[index + 1] == ',')
                            {
                                // Ended escaped field with quote, start next field.
                                fields.Add(_b.ToString());
                                _b.Clear();
                                index += 2;
                                break;
                            }
                            else
                            {
                                throw new Exception($"Invalid CSV format at {index}");
                            }
                        }
                        else // Ended escaped field with quote at end of line.
                        {
                            fields.Add(_b.ToString());
                            _b.Clear();
                            index++;
                        }
                    }
                }
            }
            else if (inputLine[index] == '\r')
            {
                throw new InvalidDataException("This function is not intended to be used with CSV data containing newlines");
            }
            else if (inputLine[index] == '\n')
            {
                throw new InvalidDataException("This function is not intended to be used with CSV data containing newlines");
            }
            else
            {
                // Parse unescaped
                // Read until comma, double quote, or end of line
                while (index < length)
                {
                    char character = inputLine[index];
                    if (character == ',')
                    {
                        fields.Add(_b.ToString());
                        _b.Clear();
                        index++;
                        break;
                    }

                    _b.Append(character);
                    index++;
                }

                if (index >= length)
                {
                    fields.Add(_b.ToString());
                    _b.Clear();
                }
            }
        }

        return fields;
    }
}
