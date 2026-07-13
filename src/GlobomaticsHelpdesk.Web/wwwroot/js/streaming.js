// Streaming chat helpers. We bypass Blazor's render diff during a token stream
// and append text directly to the DOM, then let Blazor re-render once at the
// end. This gives a smooth letter-by-letter feel instead of clumpy batched
// updates over SignalR.

export function beginStream(targetId) {
    const el = document.getElementById(targetId);
    if (!el) return;
    el.textContent = '';
}

export function appendToken(targetId, token) {
    const el = document.getElementById(targetId);
    if (!el) return;
    el.appendChild(document.createTextNode(token));

    const scroller = document.getElementById('chatMessages');
    if (scroller) {
        scroller.scrollTop = scroller.scrollHeight;
    }
}
