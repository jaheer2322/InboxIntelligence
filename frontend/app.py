import os
import requests
import streamlit as st

st.set_page_config(
    page_title="Inbox Intelligence",
    page_icon="✉️",
    layout="wide",
    initial_sidebar_state="expanded",
)

# Use Streamlit's native light theme styling. Avoid broad CSS selectors that
# override text and button colors across the entire app.
st.markdown(
    """
    <style>
      @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600;700&family=Manrope:wght@400;500;600;700;800&display=swap');

      :root {
        color-scheme: dark;
        --bg: #0b1020;
        --panel: #11182a;
        --panel-soft: #171f34;
        --border: #29334b;
        --text: #edf0ff;
        --muted: #a2acc5;
        --accent: #7667ff;
      }
      .stApp, [data-testid="stAppViewContainer"], section.main,
      .main .block-container {
        background: var(--bg) !important;
        color: var(--text) !important;
        font-family: 'DM Sans', sans-serif;
      }
      [data-testid="stHeader"] { background: rgba(11,16,32,.94) !important; }
      [data-testid="stToolbar"] { color: var(--text) !important; }
      .block-container { max-width: 1080px; padding-top: 2rem; padding-bottom: 2.5rem; }

      h1, h2, h3, p, label, .stMarkdown, [data-testid="stCaptionContainer"] {
        color: var(--text);
      }
      h1, h2, h3 { font-family: 'Manrope', sans-serif !important; letter-spacing: -.035em; }

      [data-testid="stSidebar"], [data-testid="stSidebar"] > div {
        background: #0e1526 !important;
        border-right: 1px solid var(--border);
      }
      [data-testid="stSidebar"] h1, [data-testid="stSidebar"] h2,
      [data-testid="stSidebar"] h3, [data-testid="stSidebar"] p,
      [data-testid="stSidebar"] label,
      [data-testid="stSidebar"] [data-testid="stMarkdownContainer"],
      [data-testid="stSidebar"] [data-testid="stCaptionContainer"] {
        color: var(--text) !important;
      }
      [data-testid="stSidebar"] input {
        color: var(--text) !important;
        -webkit-text-fill-color: var(--text) !important;
        background: #171f34 !important;
        border: 1px solid var(--border) !important;
      }
      [data-testid="stSidebar"] button {
        background: #171f34 !important;
        border: 1px solid #303a55 !important;
        color: var(--text) !important;
        border-radius: 12px !important;
        min-height: 44px;
        transition: background .15s ease, border-color .15s ease;
      }
      [data-testid="stSidebar"] button:hover {
        background: #252b4a !important;
        border-color: #7667ff !important;
      }
      [data-testid="stSidebar"] button p { color: var(--text) !important; }

      [data-testid="stChatInput"], [data-testid="stBottom"],
      [data-testid="stBottom"] > div {
        background: var(--bg) !important;
      }
      [data-testid="stChatInput"] > div {
        background: var(--panel) !important;
        border: 1px solid #34405d !important;
        border-radius: 16px !important;
      }
      [data-testid="stChatInput"] textarea {
        color: var(--text) !important;
        -webkit-text-fill-color: var(--text) !important;
        caret-color: #a69dff !important;
        background: transparent !important;
      }
      [data-testid="stChatInput"] textarea::placeholder {
        color: #8f9ab5 !important;
        -webkit-text-fill-color: #8f9ab5 !important;
      }
      [data-testid="stChatInput"] button {
        background: var(--accent) !important;
        color: #ffffff !important;
        border: 0 !important;
        border-radius: 10px !important;
      }
      [data-testid="stChatInput"] button svg { fill: #ffffff !important; }

      [data-testid="stChatMessage"] {
        background: var(--panel) !important;
        border: 1px solid var(--border) !important;
        border-radius: 16px !important;
        color: var(--text) !important;
      }
      div.stButton > button {
        border-radius: 12px;
        font-weight: 600;
      }

      .ii-hero {
        padding: 1.8rem 2rem;
        border-radius: 24px;
        color: #ffffff;
        background: linear-gradient(120deg, #191b35 0%, #34316d 58%, #635bff 100%);
        box-shadow: 0 18px 45px rgba(0, 0, 0, .24);
        margin-bottom: 1.5rem;
      }
      .ii-kicker {
        font-size: .75rem; text-transform: uppercase; letter-spacing: .16em;
        color: #d5d3ff; font-weight: 700; margin-bottom: .55rem;
      }
      .ii-title {
        font-family: 'Manrope', sans-serif; font-size: 2.35rem; line-height: 1.15;
        font-weight: 800; color: #ffffff; margin-bottom: .65rem;
      }
      .ii-sub { color: #eeeeff; font-size: 1rem; max-width: 660px; line-height: 1.65; }
      .ii-pill {
        display: inline-block; border: 1px solid rgba(255,255,255,.27);
        background: rgba(255,255,255,.10); border-radius: 999px;
        padding: .38rem .7rem; font-size: .76rem; margin: 1rem .35rem 0 0; color: #ffffff;
      }
      .ii-card {
        background: var(--panel); border: 1px solid var(--border); border-radius: 18px;
        padding: 1.25rem 1.4rem; box-shadow: 0 6px 24px rgba(0, 0, 0, .12);
      }
      .ii-card-label {
        color: #a8b1cb; font-size: .73rem; font-weight: 700;
        letter-spacing: .13em; text-transform: uppercase; margin-bottom: .65rem;
      }
      .ii-muted { color: var(--muted); }
    </style>
    """,
    unsafe_allow_html=True,
)

DEFAULT_API = os.getenv("INBOX_API_BASE_URL", "https://localhost:7000").rstrip("/")
ASK_PATH = "/api/Emails/ask"

if "messages" not in st.session_state:
    st.session_state.messages = []

with st.sidebar:
    st.markdown("## ✉️ Inbox Intelligence")
    st.caption("Ask questions. Find answers in your email.")
    st.divider()
    st.markdown("**Connection**")
    api_base = st.text_input(
        "API base URL",
        value=st.session_state.get("api_base", DEFAULT_API),
        help="Use the exact base URL printed when your ASP.NET Core API starts.",
    ).rstrip("/")
    st.session_state["api_base"] = api_base
    st.caption(f"Endpoint: {ASK_PATH}")
    st.divider()
    st.markdown("**Try asking**")
    examples = [
        "What decisions were made about Project Obsidian?",
        "What deadlines were mentioned in recent emails?",
        "Summarize the latest project updates.",
        "Who is responsible for staging verification?",
    ]
    for i, example in enumerate(examples):
        if st.button(example, key=f"example_{i}", use_container_width=True):
            st.session_state["pending_prompt"] = example
    st.divider()
    if st.button("＋ New conversation", use_container_width=True):
        st.session_state.messages = []
        st.session_state.pop("pending_prompt", None)
        st.rerun()
    st.caption("Powered by your .NET API, hybrid retrieval, and Gemini.")

st.markdown(
    """
    <div class="ii-hero">
      <div class="ii-kicker">YOUR PERSONAL EMAIL KNOWLEDGE LAYER</div>
      <div class="ii-title">Your inbox, understood.</div>
      <div class="ii-sub">Ask in plain English. Inbox Intelligence searches your stored emails and uses the most relevant context to generate an answer.</div>
      <span class="ii-pill">✦ Semantic + keyword search</span>
      <span class="ii-pill">↗ Context-aware answers</span>
      <span class="ii-pill">⌕ Message ID citations</span>
    </div>
    """,
    unsafe_allow_html=True,
)

heading, tag = st.columns([4, 1])
with heading:
    st.markdown("## Ask your inbox")
    st.caption("Answers are generated from the emails retrieved by your API.")
with tag:
    st.markdown("<div style='text-align:right;padding-top:1.1rem;color:#a2acc5;font-size:.8rem'>DEMO · V1</div>", unsafe_allow_html=True)

for message in st.session_state.messages:
    with st.chat_message(message["role"], avatar="🧑‍💻" if message["role"] == "user" else "✦"):
        st.markdown(message["content"])

pending = st.session_state.pop("pending_prompt", None)
prompt = st.chat_input("Ask something about your emails…")
if pending:
    prompt = pending

if prompt:
    st.session_state.messages.append({"role": "user", "content": prompt})
    with st.chat_message("user", avatar="🧑‍💻"):
        st.markdown(prompt)

    with st.chat_message("assistant", avatar="✦"):
        with st.spinner("Searching emails and generating an answer…"):
            try:
                response = requests.post(
                    f"{api_base}{ASK_PATH}",
                    json={"query": prompt},
                    timeout=90,
                    verify=True,
                )
                if not response.ok:
                    raise RuntimeError(
                        f"API returned HTTP {response.status_code}: {response.text[:1200]}"
                    )

                payload = response.json()
                answer = payload.get("answer")
                if not answer:
                    answer = "The API responded, but no `answer` field was found:\n\n" + str(payload)
                st.markdown(answer)
                st.session_state.messages.append({"role": "assistant", "content": answer})
            except requests.exceptions.SSLError:
                st.error("HTTPS certificate validation failed. Use the trusted local HTTPS URL or configure the development certificate.")
            except requests.exceptions.ConnectionError:
                st.error("Couldn't connect. Make sure your .NET API is running and the sidebar URL matches its launch address.")
            except requests.exceptions.Timeout:
                st.error("The API took too long to respond. Check the .NET logs and try again.")
            except (requests.RequestException, ValueError, RuntimeError) as exc:
                st.error(f"Request failed: {exc}")
    st.rerun()

if not st.session_state.messages:
    st.markdown(
        """
        <div class="ii-card">
          <div class="ii-card-label">START WITH A QUESTION</div>
          <div style="font-size:1.08rem;font-weight:700;color:#edf0ff;margin-bottom:.4rem">
            What decisions were made about Project Obsidian?
          </div>
          <div style="color:#a2acc5;font-size:.9rem">Choose an example from the sidebar or type your own question below.</div>
        </div>
        """,
        unsafe_allow_html=True,
    )

st.markdown(
    "<div style='text-align:center;color:#8f9ab5;font-size:.75rem;padding-top:2rem'>Inbox Intelligence · RAG demo · Verify important answers against source emails</div>",
    unsafe_allow_html=True,
)
