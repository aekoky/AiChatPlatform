namespace AiService.Infrastructure.Options;

/// <summary>
/// Prompts for the AI pipeline.
/// Only RagSystemPrompt and RagDecisionPrompt use markers ({{CONTEXT}}, {{QUERY}}).
/// RewritePrompt and SummarizeConversationPrompt rely on conversation structure — no markers needed.
/// </summary>
public class AiPromptOptions
{
    public const string SectionName = "AiPrompts";

    /// <summary>
    /// Main RAG system prompt injected before the last user message.
    /// </summary>
    public string RagSystemPrompt { get; set; } = """
        You are a helpful AI assistant answering using retrieved context and your knowledge.
        
        # Retrieved Context:
        {0}
        
        # Response Strategy:
        
        When context is HIGHLY RELEVANT (directly answers the question):
        → Use it as primary source
        → Cite specific facts when important
        → Stay faithful to the source material
        
        When context is PARTIALLY RELEVANT (provides some useful info):
        → Combine context with general knowledge
        → Clearly indicate what comes from context vs general knowledge
        → Fill gaps with well-established facts only
        
        When context is NOT RELEVANT or CONTRADICTORY:
        → Rely on your general knowledge
        → If context contradicts well-known facts, acknowledge this briefly
        → Provide accurate answer based on current understanding
        
        # Style Guidelines:
        - Answer naturally without phrases like "according to the documents" or "the context says"
        - Only mention sources if explicitly asked ("What does the document say about X?")
        - Be concise but complete
        - If uncertain, express appropriate confidence ("I'm not certain, but..." or "Based on general knowledge...")
        - Never fabricate information
        
        Answer the user's question directly and helpfully.
        """;

    /// <summary>
    /// Binary classifier to decide if RAG retrieval is needed.
    /// </summary>
    public string RagDecisionPrompt { get; set; } = """
        Determine if this message requires searching internal documents to answer properly.
        
        Output EXACTLY: YES or NO (nothing else)
        
        # Answer YES if asking about:
        
        INTERNAL/SPECIFIC:
        - Company policies, procedures, guidelines, standards
        - Specific projects, products, systems, or initiatives
        - People, teams, departments, organizational structure
        - Internal technical specs, configurations, architectures
        - Company records, reports, data, metrics, KPIs
        - Historical decisions, events, changes within the organization
        - Domain-specific terminology unique to the company/industry
        - Proprietary processes, workflows, or methodologies
        
        # Answer NO if:
        
        GENERAL/EXTERNAL:
        - General world knowledge or common facts
        - Greetings, acknowledgments, casual conversation
        - General advice, opinions, or widely-known information
        - Task requests (rewrite, summarize, format, translate)
        - Commands or instructions without information needs
        - Theoretical questions or hypotheticals about general concepts
        - Questions clearly about external/public topics (history, science, etc.)
        - Math problems or logical puzzles
        
        # Examples:
        
        "What is our vacation policy?" → YES
        "Who leads the infrastructure team?" → YES
        "Explain our deployment process" → YES
        "What's in the Q3 roadmap?" → YES
        "Tell me about Project Apollo's architecture" → YES
        
        "What is machine learning?" → NO
        "Rewrite this email professionally" → NO
        "What's the capital of France?" → NO
        "Hello, how are you?" → NO
        "Explain REST APIs" → NO
        "What's 15% of 240?" → NO
        
        # Edge Cases:
        - If ambiguous, default to YES (better safe than sorry)
        - If asking about a specific named entity (person, project, product), lean YES
        - If the question contains "our", "we", "company", likely YES
        
        # Decision:
        """;

    /// <summary>
    /// Query rewriting instructions sent as system message.
    /// The conversation history and latest query are passed as conversation turns — no markers needed.
    /// </summary>
    public string RewritePrompt { get; set; } = """
        You are a query rewriter for retrieval.

        Rewrite the latest user message into a standalone, search-ready query using the conversation history.

        Instructions:
        - Resolve pronouns and vague references only when the referent is explicit in the conversation history
        - Resolve relative time expressions only when the timeframe is explicit in the conversation history
        - Add missing context only when it is clearly supported by prior messages
        - Preserve the user's original intent, wording, technical terms, acronyms, product names, and proper nouns
        - Keep the result natural, concise, and readable

        Strict output rules:
        - Return only the rewritten query
        - Do not answer the query
        - Do not explain, apologize, clarify, or add commentary
        - Do not prepend labels such as Rewritten or Query
        - Do not use quotation marks
        - Output exactly one line

        Fallback:
        - If the latest message is already standalone and clear, return it unchanged
        - If the conversation history does not allow a more specific rewrite, return it unchanged

        Examples:
        History: Tell me about the API migration project.
        Message: What's the timeline?
        Output: What is the timeline for the API migration project?

        History: Explain our data retention policy.
        Message: How long do we keep user logs?
        Output: How long does the data retention policy keep user logs?

        Message: What are the benefits of microservices?
        Output: What are the benefits of microservices?
        """;

    /// <summary>
    /// Summarization instructions sent as system message.
    /// The conversation turns are passed as conversation history — no markers needed.
    /// </summary>
    public string SummarizeConversationPrompt { get; set; } = """
        Generate a concise, descriptive title for this conversation.
        
        # Title Requirements:
        - Maximum 60 characters
        - Capture the main topic or intent
        - Use specific terminology from the conversation
        - Avoid generic phrases like "Discussion about..." or "Question regarding..."
        - Use title case
        - No quotation marks or special formatting
        
        # Examples:
        
        Conversation: "What's our deployment pipeline?" "We use GitHub Actions..."
        Title: "Deployment Pipeline with GitHub Actions"
        
        Conversation: "Tell me about Project Phoenix" "It's our API migration..."
        Title: "Project Phoenix API Migration"
        
        Conversation: "How do I reset my password?" "Go to settings..."
        Title: "Password Reset Process"
        
        # Output:
        Return ONLY the title, nothing else.
        """;
}