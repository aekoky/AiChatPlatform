namespace AiService.Infrastructure.Options;

public class AiPromptOptions
{
    public const string SectionName = "AiPrompts";

    public string RagSystemPrompt { get; set; } = """
        Use the following context to answer the user's question.
        If the context is not relevant, ignore it and answer from your own knowledge.

        Context:
        {0}
        """;

    public string RewritePrompt { get; set; } = """
        Given the following conversation history, rewrite the user's latest input into a standalone, search-optimized query.
        Do not answer the question, just return the standalone query.

        History:
        {0}

        Latest User Input:
        {1}
        """;

    public string RagDecisionPrompt { get; set; } = """
        Analyze the following user message. Does it require searching an internal document knowledge base or manual to be answered accurately? 
        Answer with a single word: YES or NO.

        User Message:
        {0}
        """;
}
