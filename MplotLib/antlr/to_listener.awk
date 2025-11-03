BEGIN { FS=":|\\||#" }

/^expressionItem/ {
    inExpressionItem = 1
    next
}

/;/ {
    inExpressionItem = 0
    next
}

inExpressionItem && $2  {
    # strip $2 of leading and trailing whitespace
    gsub(/^[ \t]+|[ \t]+$/, "", $2)
    # print $2 ","
    # strip $3 of leading and trailing whitespace
    gsub(/^[ \t]+|[ \t]+$/, "", $3)

    printf "    public override void Enter%s(MplotParser.%sContext context)\n", $3, $3
    printf "    {\n"
    printf "        Tokens.Add(new(ExpressionTokenType.%s, context.GetText()));\n", $2
    printf "    }\n\n"

}
