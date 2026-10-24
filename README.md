# Inbox Intelligence — V1

**An Email Intelligence System Powered by Retrieval-Augmented Generation (RAG)**

Inbox Intelligence is a backend application that enables users to ask natural-language questions about the contents of an email mailbox and receive accurate, context-aware answers grounded in the emails stored in its database.

The system uses semantic search and keyword-based retrieval to identify relevant emails, retrieves additional context from related conversations, and provides the selected evidence to a Large Language Model (LLM) to generate answers with source citations.

## Retrieval Flow

The diagram below shows the current retrieval and answer-generation flow. Query rewriting and automatic contact extraction are planned enhancements and are not yet part of this flow.

```mermaid
flowchart TD
    A["User asks a question"] --> B["Streamlit frontend"]
    B --> C["POST /api/Emails/ask"]
    C --> D["EmailsController<br/>Validate question and date range"]
    D --> E["EmailService.AskAsync"]

    E --> F["Generate query embedding"]
    F --> G["Semantic search<br/>Cosine distance"]
    E --> H["Keyword search<br/>PostgreSQL full-text search"]

    G --> I[("PostgreSQL<br/>Emails + pgvector")]
    H --> I

    I --> J["Top 5 semantic results"]
    I --> K["Top 5 keyword results"]

    J --> L["Reciprocal Rank Fusion (RRF)<br/>Combine result rankings"]
    K --> L

    L --> M["Rank combined email results"]
    M --> N{"Any relevant emails?"}
    N -- "No" --> O["Return no relevant emails found"]
    N -- "Yes" --> P["Build email context<br/>Message ID, conversation ID, subject,<br/>sender, sent date, and body"]
    P --> Q["Gemini answer generation<br/>Original question + retrieved context"]
    Q --> R["Return answer with email ID citations"]

    classDef api fill:#E8F1FF,stroke:#4778C7,color:#17365D
    classDef retrieval fill:#FFF2D9,stroke:#C48A2C,color:#674500
    classDef database fill:#E5F5EA,stroke:#39845A,color:#17482B
    classDef llm fill:#F0E8FF,stroke:#8662B3,color:#39235B

    class A,B,C,D,E api
    class F,G,H,J,K,L,M retrieval
    class I database
    class P,Q,R,O llm
```

### How retrieval works

1. **Semantic search** generates an embedding for the user's question and retrieves the closest email vectors.
2. **Keyword search** uses PostgreSQL full-text search over email subjects and bodies.
3. **Contact and date filters**, when supplied, constrain both retrieval paths.
4. **Reciprocal Rank Fusion (RRF)** combines the two ranked result lists into one ranking.
5. **Answer generation** sends the original question and retrieved email context to Gemini. The model is instructed to answer from the supplied evidence and cite supporting emails using their `ExternalMessageId`.
