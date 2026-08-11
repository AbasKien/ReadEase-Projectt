/* ══════════════════════════════════════════════════════════
   TABS
══════════════════════════════════════════════════════════ */
function switchTab(name, el) {
    document.querySelectorAll('.dh-panel').forEach(p => p.classList.remove('active'));
    document.querySelectorAll('.dh-tab').forEach(t => t.classList.remove('active'));
    document.getElementById('panel-' + name).classList.add('active');
    el.classList.add('active');

    // ✅ Similar books always show alongside overview
    const similarPanel = document.getElementById('panel-similar');
    if (similarPanel) {
        similarPanel.classList.toggle('active', name === 'overview');
    }
}

/* ══════════════════════════════════════════════════════════
   DESCRIPTION EXPAND
══════════════════════════════════════════════════════════ */
let descOpen = false;
const overviewText = document.getElementById('overviewText');
function toggleDesc() {
    descOpen = !descOpen;
    if (overviewText) overviewText.style.maxHeight = descOpen ? '2400px' : '210px';
    document.getElementById('readMoreIcon').textContent = descOpen ? '▲' : '▼';
    document.getElementById('readMoreText').textContent = descOpen ? 'Read Less' : 'Read More';
}

/* ══════════════════════════════════════════════════════════
   FAVORITE
══════════════════════════════════════════════════════════ */
const favBtn = document.getElementById('favBtn');
if (favBtn) {
    favBtn.addEventListener('click', async () => {
        const res = await fetch('/Library/ToggleFavorite', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `bookId=${favBtn.dataset.id}`
        });
        if (res.ok) {
            const data = await res.json();
            favBtn.innerHTML = data.isFavorite ? '<span aria-hidden="true">❤️</span>' : '<span aria-hidden="true">🤍</span>';
            favBtn.classList.toggle('active', data.isFavorite);
        }
    });
}

/* ══════════════════════════════════════════════════════════
   HELPERS
══════════════════════════════════════════════════════════ */
let reviews = [];
const avatarPalette = ['#FF6122', '#0f3460', '#16a34a', '#7c3aed', '#0891b2', '#be185d', '#d97706'];
const escH = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const avatarColor = n => { let h = 0; for (let i = 0; i < n.length; i++) h = n.charCodeAt(i) + ((h << 5) - h); return avatarPalette[Math.abs(h) % avatarPalette.length]; };
const initials = n => (n || '?').split(' ').filter(Boolean).map(w => w[0]).join('').toUpperCase().substring(0, 2);
const timeAgo = ts => { const s = (Date.now() - ts) / 1000; if (s < 60) return 'just now'; if (s < 3600) return Math.floor(s / 60) + 'm ago'; if (s < 86400) return Math.floor(s / 3600) + 'h ago'; return Math.floor(s / 86400) + 'd ago'; };

/* ── Role badge ── */
function resolveRole(userRole, userName) {
    if (userRole === 'Admin') return 'Admin';
    if (userRole === 'Author') return 'Author';
    if (userRole === 'User') return 'User';
    if (userName && authorDisplayName && userName === authorDisplayName) return 'Author';
    return null;
}
function roleBadge(userRole, userName) {
    const role = resolveRole(userRole, userName);
    if (role === 'Admin') return `<span class="rv-role-badge badge-admin">Admin</span>`;
    if (role === 'Author') return `<span class="rv-role-badge badge-author">Author</span>`;
    if (role === 'User') return `<span class="rv-role-badge badge-user">User</span>`;
    return '';
}

/* ── Avatar HTML ── */
function avHtml(user, photo, size = 34, radius = 10) {
    if (photo) return `<img style="width:${size}px;height:${size}px;border-radius:${radius}px;object-fit:cover;flex-shrink:0;" src="${escH(photo)}" alt="${escH(user)}">`;
    return `<div style="width:${size}px;height:${size}px;border-radius:${radius}px;background:${avatarColor(user)};display:flex;align-items:center;justify-content:center;font-size:${Math.round(size * .26)}px;font-weight:900;font-family:'Nunito',sans-serif;color:#fff;flex-shrink:0;">${initials(user)}</div>`;
}

/* ══════════════════════════════════════════════════════════
   TOAST
══════════════════════════════════════════════════════════ */
function showToast(msg) {
    const t = document.getElementById('dh-toast');
    t.textContent = msg;
    t.classList.add('show');
    setTimeout(() => t.classList.remove('show'), 2600);
}

/* ══════════════════════════════════════════════════════════
   SCROLL HELPER — reliably brings a form into view
   Waits for display:flex to paint, then scrolls with extra
   bottom offset so the textarea is never hidden behind the
   browser toolbar or a sticky nav.
══════════════════════════════════════════════════════════ */
function scrollToForm(el) {
    if (!el) return;
    // Give the browser one frame to render the newly-opened form
    requestAnimationFrame(() => {
        const rect = el.getBoundingClientRect();
        const viewH = window.innerHeight;

        // Already fully visible — no scroll needed
        if (rect.top >= 80 && rect.bottom <= viewH - 60) return;

        // Scroll so the element lands ~120px from the top of the viewport
        const scrollTarget = window.scrollY + rect.top - 120;
        window.scrollTo({ top: scrollTarget, behavior: 'smooth' });
    });
}

/* ══════════════════════════════════════════════════════════
   RATING DISPLAY UPDATE
══════════════════════════════════════════════════════════ */
function updateRatingDisplay(avg, count) {
    const rounded = Math.round(avg);
    ['heroAvg', 'rvBigAvg'].forEach(id => { const el = document.getElementById(id); if (el) el.textContent = parseFloat(avg).toFixed(1); });
    const heroCnt = document.getElementById('heroCnt');
    if (heroCnt) heroCnt.textContent = `${count} rating${count !== 1 ? 's' : ''}`;
    const heroStars = document.getElementById('heroStars');
    if (heroStars) heroStars.querySelectorAll('.hs').forEach((h, i) => h.classList.toggle('on', i < rounded));
    const rvTotal = document.getElementById('rvSumTotal');
    if (rvTotal) rvTotal.textContent = `${count} rating${count !== 1 ? 's' : ''}`;
    const rvSS = document.getElementById('rvSumStars');
    if (rvSS) rvSS.querySelectorAll('span').forEach((sp, i) => sp.classList.toggle('off', i >= rounded));
}

/* ══════════════════════════════════════════════════════════
   QUICK STARS (hero)
══════════════════════════════════════════════════════════ */
const qStarEls = Array.from(document.querySelectorAll('#quickStars .qs'));
const quickLbl = document.getElementById('quickLabel');
const qLabels = ['Rate this book', "Didn't like it", 'It was OK', 'Liked it', 'Really liked it', 'It was amazing!'];
let quickRating = 0;

function applyQuickStars(n) {
    quickRating = n;
    qStarEls.forEach(x => { x.textContent = parseInt(x.dataset.q) <= n ? '★' : '☆'; x.classList.toggle('on', parseInt(x.dataset.q) <= n); x.classList.remove('hover'); });
    if (quickLbl) quickLbl.textContent = qLabels[n] || 'Rate this book';
}
qStarEls.forEach(qs => {
    qs.addEventListener('mouseenter', () => { const n = parseInt(qs.dataset.q); qStarEls.forEach(x => { x.textContent = parseInt(x.dataset.q) <= n ? '★' : '☆'; x.classList.toggle('hover', parseInt(x.dataset.q) <= n); }); if (quickLbl) quickLbl.textContent = qLabels[n]; });
    qs.addEventListener('mouseleave', () => applyQuickStars(quickRating));
    qs.addEventListener('click', async () => {
        const n = parseInt(qs.dataset.q);
        applyQuickStars(n); setFormStars(n);
        showToast(`⭐ Rated ${n} star${n > 1 ? 's' : ''} — thanks!`);
        try {
            const res = await fetch('/Library/RateBook', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ bookId, stars: n }) });
            if (res.ok) { const d = await res.json(); updateRatingDisplay(d.average, d.count); await loadReviews(); }
        } catch (e) { }
    });
});

/* ══════════════════════════════════════════════════════════
   FORM STARS
══════════════════════════════════════════════════════════ */
const fStarEls = Array.from(document.querySelectorAll('#formStars .fs'));
const fStarLabel = document.getElementById('formStarLabel');
const starLabels = ['', "Didn't like it", 'It was OK', 'Liked it', 'Really liked it', 'It was amazing!'];
let formRating = 0;

function setFormStars(n) {
    formRating = n;
    fStarEls.forEach(x => { x.textContent = parseInt(x.dataset.f) <= n ? '★' : '☆'; x.classList.toggle('on', parseInt(x.dataset.f) <= n); x.classList.remove('hover'); });
    if (fStarLabel) fStarLabel.textContent = starLabels[n] || 'Tap to rate';
}
fStarEls.forEach(fs => {
    fs.addEventListener('mouseenter', () => { const n = parseInt(fs.dataset.f); fStarEls.forEach(x => { x.textContent = parseInt(x.dataset.f) <= n ? '★' : '☆'; x.classList.toggle('hover', parseInt(x.dataset.f) <= n); }); if (fStarLabel) fStarLabel.textContent = starLabels[n]; });
    fs.addEventListener('mouseleave', () => { fStarEls.forEach(x => x.classList.remove('hover')); setFormStars(formRating); });
    fs.addEventListener('click', () => setFormStars(parseInt(fs.dataset.f)));
});

/* ══════════════════════════════════════════════════════════
   REVIEW FORM OPEN/CLOSE
══════════════════════════════════════════════════════════ */
function openReviewForm() {
    if (formRating === 0 && quickRating > 0) setFormStars(quickRating);
    const myReview = reviews.find(r => r.user === currentUser);
    if (myReview?.text?.trim()) document.getElementById('rvTextarea').value = myReview.text;
    const w = document.getElementById('rvFormWrap');
    if (!w) return;
    w.classList.add('open');
    scrollToForm(w);
    setTimeout(() => document.getElementById('rvTextarea').focus(), 300);
}
function closeReviewForm() {
    const w = document.getElementById('rvFormWrap');
    if (w) w.classList.remove('open');
    const ta = document.getElementById('rvTextarea');
    if (ta) ta.value = '';
    setFormStars(0);
}

/* ══════════════════════════════════════════════════════════
   AUTHOR REPLY (review-level)
══════════════════════════════════════════════════════════ */
function buildAuthorReplyAvatarHtml() {
    if (authorProfilePhoto) return `<img class="rv-reply-avatar" src="${escH(authorProfilePhoto)}" alt="${escH(authorDisplayName)}">`;
    return `<div class="rv-reply-avatar-initials" style="background:${avatarColor(authorDisplayName)};">${initials(authorDisplayName)}</div>`;
}

function buildAuthorReplyHtml(r) {
    if (!r.authorReply) {
        if (!isBookAuthor) return '';
        return `
        <button class="rv-author-reply-btn" onclick="openReplyForm('${r.id}',this)">💬 Reply to this review</button>
        <div class="rv-reply-form" id="replyForm-${r.id}">
            <textarea class="rv-reply-textarea" id="replyTa-${r.id}" placeholder="Write your reply as the author…" maxlength="800"></textarea>
            <div class="d-flex gap-2 justify-content-end">
                <button class="rv-reply-cancel" onclick="cancelReply('${r.id}')">Cancel</button>
                <button class="rv-reply-submit" onclick="submitReply('${r.id}')">✅ Post Reply</button>
            </div>
        </div>`;
    }
    const authorActions = isBookAuthor ? `
        <div class="d-flex gap-2 flex-shrink-0">
            <button class="rv-reply-edit-btn" onclick="editReply('${r.id}')">✏️ Edit</button>
            <button class="rv-reply-delete-btn" onclick="deleteReply('${r.id}')">🗑 Delete</button>
        </div>` : '';
    return `
    <div class="rv-reply-outer" id="replyWrap-${r.id}">
        <div class="rv-reply-card">
            <div class="d-flex align-items-center justify-content-between gap-3 mb-0">
                <div class="d-flex align-items-center gap-2 flex-grow-1 min-width-0">
                    ${buildAuthorReplyAvatarHtml()}
                    <div>
                        <div class="d-flex align-items-center gap-2 flex-wrap">
                            <span class="rv-reply-name">${escH(authorDisplayName)}</span>
                            <span class="rv-reply-author-badge">✍️ Author</span>
                        </div>
                        <span class="rv-reply-date">${r.authorRepliedTs ? timeAgo(r.authorRepliedTs) : ''}</span>
                    </div>
                </div>
                ${authorActions}
            </div>
            <div class="rv-reply-text" id="replyText-${r.id}">${escH(r.authorReply)}</div>
        </div>
        <div class="rv-reply-form" id="replyForm-${r.id}">
            <textarea class="rv-reply-textarea" id="replyTa-${r.id}" placeholder="Edit your reply…" maxlength="800">${escH(r.authorReply)}</textarea>
            <div class="d-flex gap-2 justify-content-end">
                <button class="rv-reply-cancel" onclick="cancelReply('${r.id}')">Cancel</button>
                <button class="rv-reply-submit" onclick="submitReply('${r.id}')">✅ Save Reply</button>
            </div>
        </div>
    </div>`;
}

/* ══════════════════════════════════════════════════════════
   BUILD NESTED (RECURSIVE) REPLY NODE
══════════════════════════════════════════════════════════ */
function buildReplyNode(rep, rootCid, reviewId) {
    const repId = escH(String(rep.id));
    const rid = escH(String(reviewId));
    const children = rep.replies || [];

    return `
    <div class="rv-branch-node" id="rep-${repId}">
        <div class="rv-reply-row">
            ${avHtml(rep.user, rep.userPhoto, 30, 9)}
            <div class="rv-reply-bubble flex-grow-1 min-width-0">
                <div class="rv-reply-bubble-top">
                    <span class="rv-reply-uname">${escH(rep.user)}</span>
                    ${roleBadge(rep.userRole, rep.user)}
                    ${rep.replyingTo
            ? `<span class="rv-replying-to-pill">↩ @${escH(rep.replyingTo)}</span>`
            : ''}
                    <span class="rv-reply-time ${rep.replyingTo ? '' : 'ms-auto'}">${timeAgo(rep.ts)}</span>
                </div>
                <div class="rv-reply-text-body">${escH(rep.text)}</div>
            </div>
        </div>
        <div class="rv-reply-actions">
            <button class="rv-comment-act"
                    onclick="openNestedReplyForm('${rootCid}', '${repId}', '${escH(rep.user)}', '${rid}')">
                ↩ Reply
            </button>
        </div>
        <!-- inline nested reply form for this specific reply -->
        <div class="rv-reply-form-wrap" id="rf-${rootCid}-${repId}" style="margin-left:0;">
            <div class="rv-reply-indicator">&#8617; Replying to <strong>@${escH(rep.user)}</strong></div>
            <textarea class="rv-reply-ta" id="rt-${rootCid}-${repId}"
                      placeholder="Write a reply…" maxlength="800"></textarea>
            <div class="rv-reply-form-btns">
                <button class="rv-reply-cancel-btn"
                        onclick="closeNestedReplyForm('${rootCid}', '${repId}')">Cancel</button>
                <button class="rv-reply-post-btn"
                        onclick="submitNestedReply('${rootCid}', '${repId}', '${escH(rep.user)}', '${rid}')">↩ Reply</button>
            </div>
        </div>
        ${children.length ? `
        <div class="rv-child-branch">
            ${children.map(child => buildReplyNode(child, rootCid, reviewId)).join('')}
        </div>` : ''}
    </div>`;
}

/* ══════════════════════════════════════════════════════════
   BUILD THREADED REPLIES SECTION FOR A ROOT COMMENT
══════════════════════════════════════════════════════════ */
function buildRepliesHtml(comment, reviewId) {
    const replies = comment.replies || [];
    if (!replies.length) return '';

    const cid = escH(String(comment.id));
    const count = countReplies(replies);

    return `
    <div class="rv-thread-wrap" id="thread-wrap-${cid}">
        <button class="rv-thread-toggle" id="thread-toggle-${cid}"
                onclick="toggleReplies('${cid}', ${count})">
            <span class="toggle-arrow">▶</span>
            View ${count} repl${count > 1 ? 'ies' : 'y'}
        </button>
        <div class="rv-replies-list collapsed" id="replies-list-${cid}" style="max-height:0;">
            ${replies.map(rep => buildReplyNode(rep, cid, reviewId)).join('')}
        </div>
    </div>`;
}

function countReplies(replies) {
    let n = 0;
    for (const r of replies) { n++; if (r.replies?.length) n += countReplies(r.replies); }
    return n;
}

function toggleReplies(cid, count) {
    const list = document.getElementById(`replies-list-${cid}`);
    const toggle = document.getElementById(`thread-toggle-${cid}`);
    if (!list || !toggle) return;

    const isCollapsed = list.classList.contains('collapsed');
    if (isCollapsed) {
        list.classList.remove('collapsed');
        list.classList.add('expanded');
        list.style.maxHeight = list.scrollHeight + 'px';
        toggle.classList.add('expanded');
        toggle.innerHTML = `<span class="toggle-arrow">▶</span> Hide repl${count > 1 ? 'ies' : 'y'}`;
    } else {
        list.style.maxHeight = '0';
        list.classList.remove('expanded');
        list.classList.add('collapsed');
        toggle.classList.remove('expanded');
        toggle.innerHTML = `<span class="toggle-arrow">▶</span> View ${count} repl${count > 1 ? 'ies' : 'y'}`;
    }
}

/* ══════════════════════════════════════════════════════════
   BUILD SINGLE COMMENT HTML
══════════════════════════════════════════════════════════ */
function buildCommentHtml(c, reviewId) {
    const cid = escH(String(c.id));
    const rid = escH(String(reviewId));
    const hasReplies = (c.replies || []).length > 0;

    return `
    <div class="rv-comment-item" id="comment-${cid}">
        <div class="rv-comment-main">
            ${avHtml(c.user, c.userPhoto, 34, 10)}
            <div class="rv-comment-bubble flex-grow-1">
                <div class="rv-comment-bubble-top">
                    <span class="rv-comment-name">${escH(c.user)}</span>
                    ${roleBadge(c.userRole, c.user)}
                    <span class="rv-comment-date">${timeAgo(c.ts)}</span>
                </div>
                <div class="rv-comment-text">${escH(c.text)}</div>
            </div>
        </div>
        <div class="rv-comment-actions">
            <button class="rv-comment-act ${c.likedByMe ? 'act-liked' : ''}" id="clk-${cid}" onclick="likeComment('${cid}')">
                ${c.likedByMe ? '❤️' : '🤍'} Like
                ${(c.likeCount || 0) > 0 ? `<span class="act-count">${c.likeCount}</span>` : ''}
            </button>
            <span class="rv-comment-act-sep">·</span>
            <button class="rv-comment-act" onclick="openReplyForm_comment('${cid}', '${escH(c.user)}')">↩ Reply</button>
            <span class="rv-comment-act-sep">·</span>
            <span class="rv-comment-ts">${timeAgo(c.ts)}</span>
        </div>
        ${hasReplies
            ? buildRepliesHtml(c, reviewId)
            : `<div class="rv-thread-wrap" id="thread-wrap-${cid}" style="display:none;"></div>`}
        <!-- root-level reply form (first-level replies, parentReplyId = null) -->
        <div class="rv-reply-form-wrap" id="reply-form-${cid}">
            <div class="rv-reply-indicator" id="reply-indicator-${cid}">&#8617; Replying to <strong id="reply-to-name-${cid}">@${escH(c.user)}</strong></div>
            <textarea class="rv-reply-ta" id="reply-ta-${cid}" placeholder="Write a reply…" maxlength="800"></textarea>
            <div class="rv-reply-form-btns">
                <button class="rv-reply-cancel-btn" onclick="closeReplyForm_comment('${cid}')">Cancel</button>
                <button class="rv-reply-post-btn" onclick="submitCommentReply('${cid}', '${rid}')">↩ Reply</button>
            </div>
        </div>
    </div>`;
}

/* ══════════════════════════════════════════════════════════
   REPLY FORM OPEN / CLOSE — root-level
   FIX: use scrollToForm() instead of scrollIntoView()
══════════════════════════════════════════════════════════ */
function openReplyForm_comment(commentId, replyingToUser) {
    // Close all other open forms first
    document.querySelectorAll('.rv-reply-form-wrap.open').forEach(f => f.classList.remove('open'));

    const form = document.getElementById(`reply-form-${commentId}`);
    const nameEl = document.getElementById(`reply-to-name-${commentId}`);
    if (!form) return;

    if (nameEl) nameEl.textContent = '@' + replyingToUser;
    form.dataset.replyingTo = replyingToUser;
    form.classList.add('open');

    // FIX: wait for display to apply, then scroll so form is visible
    requestAnimationFrame(() => {
        scrollToForm(form);
        setTimeout(() => document.getElementById(`reply-ta-${commentId}`)?.focus(), 150);
    });
}

function closeReplyForm_comment(commentId) {
    const form = document.getElementById(`reply-form-${commentId}`);
    if (form) {
        form.classList.remove('open');
        const ta = document.getElementById(`reply-ta-${commentId}`);
        if (ta) ta.value = '';
    }
}

/* ══════════════════════════════════════════════════════════
   NESTED REPLY FORM OPEN / CLOSE
   FIX: use scrollToForm() instead of scrollIntoView()
══════════════════════════════════════════════════════════ */
function openNestedReplyForm(rootCid, repId, replyingToUser) {
    // Close all other open forms first
    document.querySelectorAll('.rv-reply-form-wrap.open').forEach(f => f.classList.remove('open'));

    const form = document.getElementById(`rf-${rootCid}-${repId}`);
    if (!form) return;

    form.classList.add('open');

    // FIX: ensure the thread is expanded so the form is in the DOM flow
    const list = document.getElementById(`replies-list-${rootCid}`);
    if (list && list.classList.contains('collapsed')) {
        // auto-expand the thread so the form is reachable
        const toggle = document.getElementById(`thread-toggle-${rootCid}`);
        const count = list.querySelectorAll('.rv-branch-node').length;
        list.classList.remove('collapsed');
        list.classList.add('expanded');
        list.style.maxHeight = list.scrollHeight + 4000 + 'px'; // extra room for the form
        if (toggle) {
            toggle.classList.add('expanded');
            toggle.innerHTML = `<span class="toggle-arrow">▶</span> Hide repl${count > 1 ? 'ies' : 'y'}`;
        }
    } else if (list) {
        // Thread already open — bump maxHeight so the new form fits
        list.style.maxHeight = list.scrollHeight + 400 + 'px';
    }

    // FIX: scroll after paint so position is accurate
    requestAnimationFrame(() => {
        scrollToForm(form);
        setTimeout(() => document.getElementById(`rt-${rootCid}-${repId}`)?.focus(), 150);
    });
}

function closeNestedReplyForm(rootCid, repId) {
    const form = document.getElementById(`rf-${rootCid}-${repId}`);
    if (form) {
        form.classList.remove('open');
        const ta = document.getElementById(`rt-${rootCid}-${repId}`);
        if (ta) ta.value = '';
    }
}

/* ══════════════════════════════════════════════════════════
   SUBMIT COMMENT REPLY — first-level
══════════════════════════════════════════════════════════ */
async function submitCommentReply(commentId, reviewId) {
    const ta = document.getElementById(`reply-ta-${commentId}`);
    const form = document.getElementById(`reply-form-${commentId}`);
    if (!ta || !ta.value.trim()) { showToast('Reply cannot be empty.'); return; }
    const text = ta.value.trim();
    const replyingTo = form?.dataset.replyingTo || '';
    try {
        const res = await fetch('/Library/AddCommentReply', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                commentId,
                reviewId,
                text,
                replyingTo,
                parentReplyId: null
            })
        });
        const d = await res.json();
        if (res.ok && d.success) {
            closeReplyForm_comment(commentId);
            await loadReviews();
            showToast('↩ Reply posted!');
        } else { showToast('❌ ' + (d.error || 'Could not post reply.')); }
    } catch (e) { showToast('❌ Network error.'); }
}

/* ══════════════════════════════════════════════════════════
   SUBMIT NESTED REPLY
══════════════════════════════════════════════════════════ */
async function submitNestedReply(rootCid, repId, replyingToUser, reviewId) {
    const ta = document.getElementById(`rt-${rootCid}-${repId}`);
    if (!ta || !ta.value.trim()) { showToast('Reply cannot be empty.'); return; }
    const text = ta.value.trim();

    if (!reviewId) {
        const node = document.getElementById(`rep-${repId}`);
        const card = node?.closest('[id^="rvCard-"]');
        reviewId = card?.id?.replace('rvCard-', '') || '';
    }

    try {
        const res = await fetch('/Library/AddCommentReply', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                commentId: rootCid,
                reviewId: reviewId,
                text,
                replyingTo: replyingToUser,
                parentReplyId: repId
            })
        });
        const d = await res.json();
        if (res.ok && d.success) {
            closeNestedReplyForm(rootCid, repId);
            await loadReviews();
            showToast('↩ Reply posted!');
        } else { showToast('❌ ' + (d.error || 'Could not post reply.')); }
    } catch (e) { showToast('❌ Network error.'); }
}

/* ══════════════════════════════════════════════════════════
   LIKE A COMMENT
══════════════════════════════════════════════════════════ */
async function likeComment(commentId) {
    try {
        const res = await fetch('/Library/ToggleCommentLike', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(commentId)
        });
        const d = await res.json();
        if (res.ok) {
            const btn = document.getElementById(`clk-${commentId}`);
            if (btn) {
                btn.className = `rv-comment-act ${d.liked ? 'act-liked' : ''}`;
                btn.innerHTML = `${d.liked ? '❤️' : '🤍'} Like${d.count > 0 ? ` <span class="act-count">${d.count}</span>` : ''}`;
                btn.setAttribute('onclick', `likeComment('${commentId}')`);
            }
        }
    } catch (e) { showToast('❌ Network error.'); }
}

/* ══════════════════════════════════════════════════════════
   RENDER REVIEWS
══════════════════════════════════════════════════════════ */
function renderReviews() {
    const list = document.getElementById('rvList');
    const cntEl = document.getElementById('rvListCnt');
    cntEl.textContent = `${reviews.length} review${reviews.length !== 1 ? 's' : ''}`;

    if (!reviews.length) {
        list.innerHTML = `<div class="rv-empty"><span class="rv-empty-icon">💬</span>No reviews yet — be the first to share your thoughts!</div>`;
        updateBars(); return;
    }

    list.innerHTML = reviews.map(r => {
        const isMe = currentUser && r.user === currentUser;
        const hasText = r.text && r.text.trim().length > 0;
        const rid = escH(String(r.id));
        const commentsHtml = (r.comments || []).map(c => buildCommentHtml(c, r.id)).join('');

        return `
        <div class="rv-card" id="rvCard-${rid}">
            <div class="rv-card-hdr">
                ${r.userPhoto
                ? `<img class="rv-avatar" src="${escH(r.userPhoto)}" style="object-fit:cover;">`
                : `<div class="rv-avatar" style="background:${avatarColor(r.user)};">${initials(r.user)}</div>`}
                <div class="flex-grow-1 min-width-0">
                    <div class="rv-card-name">
                        ${escH(r.user)}
                        ${roleBadge(r.userRole, r.user)}
                        ${isMe ? '<span class="rv-card-you">You</span>' : ''}
                    </div>
                    <div class="d-flex align-items-center gap-2 mt-1">
                        <div class="rv-card-stars">
                            ${[1, 2, 3, 4, 5].map(i => `<span class="${i <= r.stars ? '' : 'off'}">★</span>`).join('')}
                        </div>
                        <span class="rv-card-date">${timeAgo(r.ts)}</span>
                    </div>
                </div>
            </div>
            ${hasText ? `<div class="rv-card-body">${escH(r.text)}</div>` : ''}
            ${buildAuthorReplyHtml(r)}
            <div class="rv-action-bar">
                <button id="like-btn-${rid}" onclick="toggleLike('${rid}')"
                        class="rv-act-btn ${r.likedByMe ? 'liked' : ''}">
                    ${r.likedByMe ? '❤️' : '🤍'}
                    <span class="rv-act-badge" id="like-count-${rid}">${r.likeCount || 0}</span>
                </button>
                <button onclick="toggleCommentForm('${rid}')" class="rv-act-btn">
                    💬 Comment
                    ${(r.comments || []).length > 0 ? `<span class="rv-act-badge">${r.comments.length}</span>` : ''}
                </button>
            </div>
            <div class="rv-comments-section" id="comments-${rid}">${commentsHtml}</div>
            <div id="comment-form-${rid}" class="rv-comment-form">
                <textarea id="comment-ta-${rid}" class="rv-comment-ta"
                          placeholder="Write a comment…" maxlength="800"></textarea>
                <div class="d-flex gap-2 justify-content-end">
                    <button onclick="toggleCommentForm('${rid}')" class="rv-reply-cancel-btn">Cancel</button>
                    <button onclick="submitComment('${rid}')" class="rv-reply-post-btn">Post Comment</button>
                </div>
            </div>
        </div>`;
    }).join('');

    updateBars();
}

/* ══════════════════════════════════════════════════════════
   LIKE REVIEW
══════════════════════════════════════════════════════════ */
async function toggleLike(rid) {
    try {
        const res = await fetch('/Library/ToggleLike', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(rid) });
        const d = await res.json();
        if (res.ok) {
            const btn = document.getElementById(`like-btn-${rid}`);
            if (btn) btn.outerHTML = `<button id="like-btn-${rid}" onclick="toggleLike('${rid}')" class="rv-act-btn ${d.liked ? 'liked' : ''}">${d.liked ? '❤️' : '🤍'} <span class="rv-act-badge">${d.count}</span></button>`;
        }
    } catch (e) { showToast('❌ Network error.'); }
}

/* ══════════════════════════════════════════════════════════
   TOP-LEVEL COMMENT FORM
   FIX: use scrollToForm() here too
══════════════════════════════════════════════════════════ */
function toggleCommentForm(rid) {
    document.querySelectorAll('[id^="comment-form-"]').forEach(f => { if (f.id !== `comment-form-${rid}`) f.classList.remove('open'); });
    const form = document.getElementById(`comment-form-${rid}`);
    if (!form) return;
    form.classList.toggle('open');
    if (form.classList.contains('open')) {
        requestAnimationFrame(() => {
            scrollToForm(form);
            setTimeout(() => document.getElementById(`comment-ta-${rid}`)?.focus(), 150);
        });
    }
}

async function submitComment(rid) {
    const ta = document.getElementById(`comment-ta-${rid}`);
    if (!ta || !ta.value.trim()) { showToast('Comment cannot be empty.'); return; }
    const text = ta.value.trim();
    try {
        const res = await fetch('/Library/AddComment', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ reviewId: rid, text }) });
        const d = await res.json();
        if (res.ok && d.success) { ta.value = ''; document.getElementById(`comment-form-${rid}`).classList.remove('open'); await loadReviews(); showToast('💬 Comment posted!'); }
        else showToast('❌ ' + (d.error || 'Could not post comment.'));
    } catch (e) { showToast('❌ Network error.'); }
}

/* ══════════════════════════════════════════════════════════
   RATING BARS
══════════════════════════════════════════════════════════ */
function updateBars() {
    const counts = { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 };
    reviews.forEach(r => counts[r.stars]++);
    const total = reviews.length || 1;
    document.querySelectorAll('.rv-bar-fill').forEach(b => { b.style.width = Math.round((counts[parseInt(b.dataset.star)] / total) * 100) + '%'; });
    document.querySelectorAll('[data-star-count]').forEach(el => { el.textContent = counts[parseInt(el.dataset.starCount)]; });
}

/* ══════════════════════════════════════════════════════════
   SUBMIT REVIEW
══════════════════════════════════════════════════════════ */
async function submitReview() {
    if (formRating === 0) { showToast('⭐ Please select a star rating first.'); return; }
    const ratingToSave = formRating;
    const textToSave = document.getElementById('rvTextarea').value.trim();
    const idx = reviews.findIndex(r => r.user === currentUser);
    const entry = { id: idx >= 0 ? reviews[idx].id : null, user: currentUser, userRole: currentUserRole, stars: ratingToSave, text: textToSave, ts: Date.now(), authorReply: idx >= 0 ? reviews[idx].authorReply : null, authorRepliedTs: idx >= 0 ? reviews[idx].authorRepliedTs : null, likeCount: idx >= 0 ? reviews[idx].likeCount : 0, likedByMe: idx >= 0 ? reviews[idx].likedByMe : false, comments: idx >= 0 ? reviews[idx].comments : [] };
    if (idx >= 0) reviews[idx] = entry; else reviews.unshift(entry);
    renderReviews(); closeReviewForm(); applyQuickStars(ratingToSave);
    showToast('🎉 Review posted — thank you!');
    try {
        const res = await fetch('/Library/AddReview', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ bookId, stars: ratingToSave, text: textToSave }) });
        const d = await res.json();
        if (res.ok && d.success) { updateRatingDisplay(d.average, d.count); if (d.reviews) { reviews = d.reviews; renderReviews(); } }
        else { showToast('❌ ' + (d.error || 'Could not save review.')); await loadReviews(); }
    } catch (e) { showToast('❌ Network error.'); await loadReviews(); }
}

/* ══════════════════════════════════════════════════════════
   AUTHOR REPLY ACTIONS (review-level)
══════════════════════════════════════════════════════════ */
function openReplyForm(reviewId, triggerBtn) {
    if (triggerBtn) triggerBtn.style.display = 'none';
    const form = document.getElementById(`replyForm-${reviewId}`);
    if (form) {
        form.classList.add('open');
        requestAnimationFrame(() => {
            scrollToForm(form);
            const ta = document.getElementById(`replyTa-${reviewId}`);
            if (ta) { setTimeout(() => { ta.focus(); ta.setSelectionRange(ta.value.length, ta.value.length); }, 150); }
        });
    }
}
function cancelReply(reviewId) {
    const form = document.getElementById(`replyForm-${reviewId}`);
    if (form) form.classList.remove('open');
    const card = document.getElementById(`rvCard-${reviewId}`);
    if (card) { const btn = card.querySelector('.rv-author-reply-btn'); if (btn) btn.style.display = ''; }
    const r = reviews.find(rv => String(rv.id) === String(reviewId));
    const ta = document.getElementById(`replyTa-${reviewId}`);
    if (ta && r) ta.value = r.authorReply || '';
}
function editReply(reviewId) {
    const form = document.getElementById(`replyForm-${reviewId}`);
    if (form) {
        form.classList.add('open');
        requestAnimationFrame(() => {
            scrollToForm(form);
            const ta = document.getElementById(`replyTa-${reviewId}`);
            if (ta) { setTimeout(() => { ta.focus(); ta.setSelectionRange(ta.value.length, ta.value.length); }, 150); }
        });
    }
}
async function submitReply(reviewId) {
    const ta = document.getElementById(`replyTa-${reviewId}`);
    const text = ta ? ta.value.trim() : '';
    if (!text) { showToast('💬 Reply cannot be empty.'); return; }
    try {
        const res = await fetch('/Library/ReplyToReview', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ reviewId, replyText: text }) });
        const d = await res.json();
        if (res.ok && d.success) { showToast('✅ Reply posted!'); await loadReviews(); }
        else showToast('❌ ' + (d.error || 'Could not post reply.'));
    } catch (e) { showToast('❌ Network error.'); }
}
async function deleteReply(reviewId) {
    if (!confirm('Remove your reply from this review?')) return;
    try {
        const res = await fetch('/Library/DeleteReply', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(reviewId) });
        const d = await res.json();
        if (res.ok && d.success) { showToast('🗑 Reply removed.'); await loadReviews(); }
        else showToast('❌ ' + (d.error || 'Could not remove reply.'));
    } catch (e) { showToast('❌ Network error.'); }
}

/* ══════════════════════════════════════════════════════════
   LOAD REVIEWS
══════════════════════════════════════════════════════════ */
async function loadReviews() {
    try {
        const res = await fetch(`/Library/GetReviews?bookId=${bookId}`);
        const d = await res.json();
        if (res.ok && d.success) {
            reviews = d.reviews || [];
            renderReviews();
            if (d.average !== undefined) updateRatingDisplay(d.average, d.count);
            if (!isBookAuthor && (d.myStars || 0) > 0) { applyQuickStars(d.myStars); setFormStars(d.myStars); }
        }
    } catch (e) { console.error('GetReviews error:', e); }
}

loadReviews();


