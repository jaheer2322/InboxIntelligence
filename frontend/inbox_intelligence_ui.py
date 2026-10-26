import os
import requests
import streamlit as st
from urllib.parse import urlsplit

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
      @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600;700&family=Space+Grotesk:wght@400;500;600;700&display=swap');
      :root { color-scheme: dark; --bg:#090b0f; --surface:#10141b; --line:#29313b; --text:#f1f4f7; --muted:#929daa; --accent:#b6f36b; }
      .stApp,[data-testid="stAppViewContainer"],section.main,.main .block-container { background:var(--bg)!important;color:var(--text)!important;font-family:'DM Sans',sans-serif; }
      [data-testid="stHeader"] {
        background: #090b0f !important;
        border-bottom: 1px solid #202630 !important;
        z-index: 1000 !important;
      }
      [data-testid="stToolbar"] { background: #090b0f !important; }
      .block-container {
        max-width: 1180px;
        padding-top: 4.25rem !important;
        padding-bottom: 7rem;
      }
      html { scroll-behavior: smooth; }
      h1,h2,h3 { font-family:'Space Grotesk',sans-serif!important;letter-spacing:-.045em;color:var(--text)!important; }
      p,label,.stMarkdown,[data-testid="stCaptionContainer"] { color:var(--text); }
      [data-testid="stCaptionContainer"] p { color:var(--muted)!important; }
      [data-testid="stSidebar"],[data-testid="stSidebar"]>div { background:#0d1015!important;border-right:1px solid #202630; }
      [data-testid="stSidebar"] [data-testid="stMarkdownContainer"],[data-testid="stSidebar"] h1,[data-testid="stSidebar"] h2,[data-testid="stSidebar"] h3,[data-testid="stSidebar"] p,[data-testid="stSidebar"] label { color:var(--text)!important; }
      [data-testid="stSidebar"] [data-testid="stMarkdownContainer"] p { color:var(--muted)!important; }
      [data-testid="stSidebar"] hr { border-color:#252c36!important;margin:1.1rem 0!important; }
      [data-testid="stSidebar"] input { color:var(--text)!important;-webkit-text-fill-color:var(--text)!important;background:#11161d!important;border:1px solid #303945!important;border-radius:8px!important; }
      [data-testid="stSidebar"] button { background:transparent!important;border:1px solid #252d37!important;color:var(--text)!important;border-radius:8px!important;min-height:44px;text-align:left!important;transition:all .15s ease; }
      [data-testid="stSidebar"] button:hover { background:#171e26!important;border-color:#8ebf52!important; }
      [data-testid="stSidebar"] button p { color:var(--text)!important;font-size:.83rem; }
      [data-testid="stChatInput"],[data-testid="stBottom"],[data-testid="stBottom"]>div { background:var(--bg)!important; }
      [data-testid="stBottom"] {
        width: 100% !important;
        left: 0 !important;
        right: 0 !important;
      }
      [data-testid="stBottom"] > div {
        width: 100% !important;
        max-width: 100% !important;
        margin: 0 auto !important;
        padding: 0 1rem !important;
        box-sizing: border-box !important;
      }
      [data-testid="stChatInput"] {
        width: min(760px, calc(100vw - 2rem)) !important;
        max-width: 760px !important;
        margin: 0 auto !important;
      }
      [data-testid="stChatInput"] > div {
        width: 100% !important;
        box-sizing: border-box !important;
      }
      @media (max-width: 800px) {
        [data-testid="stChatInput"] {
          width: calc(100vw - 2rem) !important;
          max-width: calc(100vw - 2rem) !important;
        }
      }
      [data-testid="stChatInput"]>div { background:#11161d!important;border:1px solid #38434e!important;border-radius:12px!important;box-shadow:0 12px 32px rgba(0,0,0,.25); }
      [data-testid="stChatInput"]>div:focus-within { border-color:var(--accent)!important; }
      [data-testid="stChatInput"] textarea { color:var(--text)!important;-webkit-text-fill-color:var(--text)!important;caret-color:var(--accent)!important;background:transparent!important; }
      [data-testid="stChatInput"] textarea::placeholder { color:#7f8a97!important;-webkit-text-fill-color:#7f8a97!important; }
      [data-testid="stChatInput"] button { background:var(--accent)!important;color:#10150b!important;border:0!important;border-radius:8px!important; }
      [data-testid="stChatInput"] button svg { fill:#10150b!important; }
      [data-testid="stChatMessage"] { background:#10141b!important;border:1px solid #252d37!important;border-radius:12px!important;color:var(--text)!important;padding:.8rem 1rem;margin-bottom:.75rem; }
      div.stButton>button { border-radius:8px;font-weight:600; }
      .ii-topline { display:flex;align-items:center;justify-content:space-between;gap:1rem;border-bottom:1px solid #252c35;padding:.25rem 0 1rem;margin-bottom:2rem; }
      .ii-brand { display:flex;align-items:center;gap:.7rem;color:#f4f6f8;font:700 .9rem 'Space Grotesk',sans-serif;letter-spacing:-.02em; }
      .ii-mark { display:grid;place-items:center;width:30px;height:30px;border-radius:8px;color:#11160b;background:var(--accent);font-weight:800;font-size:1rem; }
      .ii-status { display:inline-flex;align-items:center;gap:.45rem;color:#b9c3cd;font-size:.72rem;text-transform:uppercase;letter-spacing:.12em; }
      .ii-dot { width:7px;height:7px;border-radius:50%;background:var(--accent);display:inline-block; }
      .ii-hero { padding:0 0 2rem;margin-bottom:2rem;border-bottom:1px solid #252c35; }
      .ii-kicker { color:var(--accent);font:700 .7rem 'Space Grotesk',sans-serif;letter-spacing:.18em;text-transform:uppercase;margin-bottom:1rem; }
      .ii-title { color:#f4f6f8;font:700 clamp(2.6rem,6vw,4.7rem)/.99 'Space Grotesk',sans-serif;letter-spacing:-.075em;max-width:800px;margin:0 0 1.2rem; }
      .ii-title span { color:var(--accent); }
      .ii-sub { color:#a9b2bd;font-size:1rem;max-width:620px;line-height:1.8; }
      .ii-pills { display:flex;flex-wrap:wrap;gap:.55rem;margin-top:1.4rem; }
      .ii-pill { display:inline-flex;align-items:center;gap:.4rem;padding:.45rem .65rem;border:1px solid #303945;border-radius:6px;background:#11161d;color:#c4ccd4;font-size:.72rem;letter-spacing:.01em; }
      .ii-section-label { color:#8793a0;font:700 .68rem 'Space Grotesk',sans-serif;text-transform:uppercase;letter-spacing:.16em;margin-bottom:.5rem; }
      .ii-empty { border:1px solid #29313b;border-radius:14px;background:#0e1218;padding:1.5rem;margin-top:.75rem; }
      .ii-empty-title { color:#f1f4f7;font:600 1.15rem 'Space Grotesk',sans-serif;margin-bottom:.45rem; }
      .ii-empty-copy { color:#929daa;font-size:.88rem;line-height:1.7; }
      .ii-tip { border-left:2px solid var(--accent);padding:.65rem .9rem;color:#9da8b4;background:#10150f;font-size:.8rem;margin-top:1rem; }
      .ii-footer { border-top:1px solid #252c35;margin-top:3rem;padding-top:1rem;color:#65717d;font-size:.72rem;letter-spacing:.03em; }
      @media(max-width:700px) { .block-container{padding-top:1rem}.ii-title{font-size:2.65rem}.ii-topline{align-items:flex-start} }
    
      .ii-thinking {
        display: flex; align-items: center; gap: 1rem;
        border: 1px solid #35402d; border-radius: 12px;
        background: linear-gradient(110deg, #10150f, #11161b 65%);
        padding: 1.1rem 1.2rem; margin: .25rem 0 .75rem;
      }
      .ii-thinking-mark {
        display: flex; align-items: center; justify-content: center; gap: 4px;
        width: 42px; height: 42px; flex: 0 0 42px;
        border: 1px solid #435b32; border-radius: 10px; background: #192414;
      }
      .ii-thinking-mark span {
        display: block; width: 4px; border-radius: 4px; background: #b6f36b;
        animation: ii-pulse 1.05s ease-in-out infinite;
      }
      .ii-thinking-mark span:nth-child(1) { height: 10px; animation-delay: 0s; }
      .ii-thinking-mark span:nth-child(2) { height: 19px; animation-delay: .15s; }
      .ii-thinking-mark span:nth-child(3) { height: 13px; animation-delay: .3s; }
      .ii-thinking-title { color: #f1f4f7; font: 600 .95rem 'Space Grotesk', sans-serif; margin-bottom: .25rem; }
      .ii-thinking-sub { color: #929daa; font-size: .78rem; line-height: 1.5; }
      .ii-thinking-track { height: 3px; background: #273124; border-radius: 5px; overflow: hidden; margin-top: .8rem; }
      .ii-thinking-progress {
        height: 100%; width: 38%; border-radius: 5px; background: #b6f36b;
        animation: ii-progress 1.8s ease-in-out infinite;
      }
      @keyframes ii-pulse { 0%,100% { transform: scaleY(.65); opacity: .65; } 50% { transform: scaleY(1); opacity: 1; } }
      @keyframes ii-progress { 0% { transform: translateX(-110%); } 100% { transform: translateX(290%); } }
      @media (prefers-reduced-motion: reduce) {
        .ii-thinking-mark span, .ii-thinking-progress { animation: none; }
      }
</style>
    """,
    unsafe_allow_html=True,
)

DEFAULT_API = os.getenv("INBOX_API_BASE_URL", "https://localhost:7089").rstrip("/")
ASK_PATH = "/api/v2/Emails/ask"

if "messages" not in st.session_state:
    st.session_state.messages = []

with st.sidebar:
    st.markdown("## ✉️ Inbox Intelligence")
    st.caption("Ask questions. Find answers in your email.")
    st.divider()
    st.markdown("**Connection**")
    saved_api_base = st.session_state.get("api_base")
    # Migrate the prior default: port 7089 is the HTTPS endpoint in launchSettings.json.
    if not saved_api_base or saved_api_base == "http://localhost:7089":
        saved_api_base = DEFAULT_API
    api_base = st.text_input(
        "API base URL",
        value=saved_api_base,
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
    def choose_example(question):
        st.session_state["pending_prompt"] = question

    for i, example in enumerate(examples):
        st.button(
            example,
            key=f"example_{i}",
            use_container_width=True,
            on_click=choose_example,
            args=(example,),
        )
    st.divider()
    if st.button("＋ New conversation", use_container_width=True):
        st.session_state.messages = []
        st.session_state.pop("pending_prompt", None)
        st.rerun()
    st.caption("Powered by your .NET API, hybrid retrieval, and Gemini.")

st.markdown(
    """
    <div class="ii-topline">
      <div class="ii-brand"><span class="ii-mark">i.</span> INBOX INTELLIGENCE</div>
      <div class="ii-status"><span class="ii-dot"></span> KNOWLEDGE SYSTEM / V2</div>
    </div>
    <div class="ii-hero">
      <div class="ii-kicker">PERSONAL EMAIL SEARCH</div>
      <div class="ii-title">Less searching.<br><span>More knowing.</span></div>
      <div class="ii-sub">Turn your email archive into a knowledge base. Ask a question in plain language and get an answer grounded in the messages that matter.</div>
      <div class="ii-pills">
        <span class="ii-pill">↗ Hybrid retrieval</span>
        <span class="ii-pill">⌁ Conversation context</span>
        <span class="ii-pill"># Source message IDs</span>
      </div>
    </div>
    """,
    unsafe_allow_html=True,
)

heading, tag = st.columns([4, 1])
with heading:
    st.markdown("## Ask a question")
    st.caption("Search across your stored emails using natural language.")
with tag:
    st.markdown("<div style='text-align:right;padding-top:1.15rem;color:#82908d;font-size:.7rem;letter-spacing:.14em'>READY WHEN YOU ARE</div>", unsafe_allow_html=True)

for message in st.session_state.messages:
    with st.chat_message(message["role"], avatar="user" if message["role"] == "user" else "assistant"):
        st.markdown(message["content"])

st.components.v1.html(
    """
    <script>
      function scrollInboxToLatest() {
        const doc = window.parent.document;
        const main = doc.querySelector('section.main');
        if (main) main.scrollTo({ top: main.scrollHeight, behavior: 'smooth' });
        window.parent.scrollTo({ top: doc.body.scrollHeight, behavior: 'smooth' });
      }
      setTimeout(scrollInboxToLatest, 150);
      setTimeout(scrollInboxToLatest, 500);
    </script>
    """,
    height=0,
)

chat_prompt = st.chat_input("Ask something about your emails…")

# Sidebar selection takes priority; otherwise use the submitted chat input.
prompt = st.session_state.get("pending_prompt") or chat_prompt

if prompt:
    # Clear the selected sidebar prompt only after the handler has received it.
    st.session_state.pop("pending_prompt", None)

    st.session_state.messages.append({"role": "user", "content": prompt})
    with st.chat_message("user", avatar="user"):
        st.markdown(prompt)

    with st.chat_message("assistant", avatar="assistant"):
        loading_placeholder = st.empty()
        loading_placeholder.markdown(
            """
            <div class="ii-thinking">
              <div class="ii-thinking-mark"><span></span><span></span><span></span></div>
              <div class="ii-thinking-copy">
                <div class="ii-thinking-title">Searching your inbox</div>
                <div class="ii-thinking-sub">Finding relevant messages and assembling the context for your answer.</div>
                <div class="ii-thinking-track"><div class="ii-thinking-progress"></div></div>
              </div>
            </div>
            """,
            unsafe_allow_html=True,
        )
        try:
            api_host = (urlsplit(api_base).hostname or "").lower()
            ca_bundle = os.getenv("INBOX_API_CA_BUNDLE")
            # Keep the existing development-certificate behavior.
            verify_tls = ca_bundle if ca_bundle else api_host not in {"localhost", "127.0.0.1", "::1"}
            response = requests.post(
                f"{api_base}{ASK_PATH}",
                json={"query": prompt},
                headers={"api-version": "2.0"},
                timeout=120,
                verify=verify_tls,
            )
            if not response.ok:
                raise RuntimeError(
                    f"API returned HTTP {response.status_code}: {response.text[:1200]}"
                )

            payload = response.json()
            answer = payload.get("answer")
            if not answer:
                answer = "The API responded, but no `answer` field was found:\n\n" + str(payload)
            loading_placeholder.empty()
            st.markdown(answer)
            st.session_state.messages.append({"role": "assistant", "content": answer})
        except requests.exceptions.SSLError:
            loading_placeholder.empty()
            error = "HTTPS certificate validation failed. Use the trusted local HTTPS URL or configure the development certificate."
            st.error(error)
            st.session_state.messages.append({"role": "assistant", "content": error})
        except requests.exceptions.ConnectionError:
            loading_placeholder.empty()
            error = "Couldn't connect. Make sure your .NET API is running and the sidebar URL matches its launch address."
            st.error(error)
            st.session_state.messages.append({"role": "assistant", "content": error})
        except requests.exceptions.Timeout:
            loading_placeholder.empty()
            error = "The API took too long to respond. Check the .NET logs and try again."
            st.error(error)
            st.session_state.messages.append({"role": "assistant", "content": error})
        except (requests.RequestException, ValueError, RuntimeError) as exc:
            loading_placeholder.empty()
            error = f"Request failed: {exc}"
            st.error(error)
            st.session_state.messages.append({"role": "assistant", "content": error})
    st.rerun()

if not st.session_state.messages:
    st.markdown(
        """
        <div class="ii-empty">
          <div class="ii-section-label">A GOOD PLACE TO START</div>
          <div class="ii-empty-title">What happened on Project Obsidian?</div>
          <div class="ii-empty-copy">Ask about decisions, owners, timelines, blockers, or updates. Your API will retrieve relevant messages and generate an answer from their context.</div>
          <div class="ii-tip">TIP&nbsp;&nbsp; Be specific about a project, person, date, or decision to improve retrieval.</div>
        </div>
        """,
        unsafe_allow_html=True,
    )

st.markdown(
    "<div class='ii-footer'>INBOX INTELLIGENCE &nbsp; / &nbsp; RAG DEMO &nbsp; / &nbsp; VERIFY IMPORTANT ANSWERS AGAINST SOURCE EMAILS</div>",
    unsafe_allow_html=True,
)
