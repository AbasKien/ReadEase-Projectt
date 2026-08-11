// authorprofile.js
// authorUserId, currentUserId, isOwnProfile are set inline in AuthorProfile.cshtml

function switchTab(name, btn) {
    document.querySelectorAll('.ap-tab').forEach(b => b.classList.remove('active'));
    document.querySelectorAll('.ap-panel').forEach(p => p.classList.remove('active'));
    btn.classList.add('active');
    document.getElementById('tab-' + name).classList.add('active');
}

async function toggleFollow() {
    const btn = document.getElementById('followBtn');
    btn.disabled = true;
    try {
        const res = await fetch('/Posts/ToggleFollow', {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ followedId: authorUserId })
        });
        const data = await res.json();
        btn.classList.toggle('following', data.isFollowing);
        btn.textContent = data.isFollowing ? '✓ Following' : '+ Follow';
        const countEl = document.getElementById('followersCount');
        const current = parseInt(countEl.textContent) || 0;
        countEl.textContent = data.isFollowing ? current + 1 : Math.max(0, current - 1);
    } finally { btn.disabled = false; }
}

document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('postInput')?.addEventListener('input', function () {
        document.getElementById('postCharCount').textContent = this.value.length;
    });
});

async function submitPost() {
    const input = document.getElementById('postInput');
    const content = input.value.trim();
    if (!content) return;
    const btn = document.getElementById('postBtn');
    btn.disabled = true;
    try {
        const res = await fetch('/Posts/CreatePost', {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ content, wallOwnerId: authorUserId })
        });
        if (!res.ok) { alert('Failed to post.'); return; }
        const post = await res.json();
        input.value = '';
        document.getElementById('postCharCount').textContent = '0';
        document.getElementById('emptyPosts')?.remove();
        document.getElementById('postList').insertAdjacentHTML('afterbegin', buildPostCard(post));
    } finally { btn.disabled = false; }
}

async function deletePost(postId) {
    if (!confirm('Delete this post?')) return;
    const res = await fetch('/Posts/DeletePost', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ postId })
    });
    if (res.ok) document.getElementById('post-' + postId)?.remove();
}

async function toggleLike(postId, btn) {
    const res = await fetch('/Posts/ToggleLike', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ postId })
    });
    const data = await res.json();
    btn.classList.toggle('liked', data.isLiked);
    const heartFill = data.isLiked ? '#e74c3c' : 'none';
    const heartStroke = data.isLiked ? '#e74c3c' : 'currentColor';
    btn.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="' + heartFill + '" stroke="' + heartStroke + '" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/></svg> <span class="like-count">' + data.likeCount + '</span>';
}

function toggleComments(postId, btn) {
    const section = document.getElementById('comments-' + postId);
    const inputRow = document.getElementById('comment-input-row-' + postId);
    const isHidden = section.style.display === 'none';
    section.style.display = isHidden ? 'block' : 'none';
    if (inputRow) inputRow.style.display = isHidden ? 'flex' : 'none';
    if (isHidden) { var input = document.getElementById('comment-input-' + postId); if (input) input.focus(); }
}

async function submitComment(postId) {
    const input = document.getElementById('comment-input-' + postId);
    const content = input.value.trim();
    if (!content) return;
    const res = await fetch('/Posts/AddComment', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ postId, content })
    });
    if (!res.ok) return;
    const comment = await res.json();
    input.value = '';
    document.getElementById('comment-list-' + postId).insertAdjacentHTML('beforeend', buildCommentHtml(comment, postId));
    updateReplyBadge(postId, 1);
}

function updateReplyBadge(postId, delta) {
    var postCard = document.getElementById('post-' + postId);
    if (!postCard) return;
    var btn = postCard.querySelector('.reply-count-btn');
    if (!btn) return;
    var badge = btn.querySelector('.reply-badge');
    if (!badge) {
        badge = document.createElement('span');
        badge.className = 'reply-badge';
        badge.style.cssText = 'font-size:.68rem;background:rgba(242,101,34,.12);color:#f26522;padding:.08rem .45rem;border-radius:50px;font-family:Nunito,sans-serif;font-weight:800;margin-left:.2rem;';
        btn.appendChild(badge);
    }
    badge.textContent = (parseInt(badge.textContent) || 0) + delta;
}

// commentId = which reply box to show/hide
// rootCommentId = the top-level comment to save under (passed from button)
function showReplyBox(commentId, replyToName, rootCommentId, postId) {
    document.querySelectorAll('[id^="reply-box-"]').forEach(function (el) {
        if (el.id !== 'reply-box-' + commentId) el.style.display = 'none';
    });
    const box = document.getElementById('reply-box-' + commentId);
    const isHidden = box.style.display === 'none';
    box.style.display = isHidden ? 'flex' : 'none';
    if (isHidden) {
        const input = document.getElementById('reply-input-' + commentId);
        input.placeholder = 'Reply to ' + replyToName + '...';
        input.dataset.replyToName = replyToName;
        // Update send button to use rootCommentId so DB always saves flat under root
        const sendBtn = input.nextElementSibling;
        if (sendBtn) sendBtn.setAttribute('onclick', 'submitReply("' + (postId || '') + '","' + (rootCommentId || commentId) + '","' + commentId + '")');
        input.focus();
    }
}

// postId = the post
// rootCommentId = top-level comment to save under in DB (always flat)
// displayParentId = which input box we're reading from (visual only)
async function submitReply(postId, rootCommentId, displayParentId) {
    var input = document.getElementById('reply-input-' + displayParentId);
    var content = input.value.trim();
    if (!content) return;
    var replyToName = input.dataset.replyToName || '';
    const res = await fetch('/Posts/AddComment', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ postId, content, parentCommentId: rootCommentId, replyToName })
    });
    if (!res.ok) return;
    const reply = await res.json();
    input.value = '';
    document.getElementById('reply-box-' + displayParentId).style.display = 'none';

    // Always insert under rootCommentId's replies list
    var repliesDiv = document.getElementById('replies-' + rootCommentId);
    if (!repliesDiv) {
        var parentEl = document.getElementById('comment-' + rootCommentId);
        var innerDiv = parentEl ? (parentEl.querySelector('div[style*="flex:1"]') || parentEl.lastElementChild) : null;
        var toggleBtn = document.createElement('button');
        toggleBtn.className = 'toggle-replies-btn';
        toggleBtn.id = 'toggle-' + rootCommentId;
        toggleBtn.setAttribute('onclick', 'toggleReplies("' + rootCommentId + '", this)');
        toggleBtn.innerHTML = '&#9660; 1 reply';
        repliesDiv = document.createElement('div');
        repliesDiv.className = 'replies-list';
        repliesDiv.id = 'replies-' + rootCommentId;
        if (innerDiv) {
            var composeBox = document.getElementById('reply-box-' + rootCommentId);
            if (composeBox && composeBox.parentNode === innerDiv) {
                innerDiv.insertBefore(toggleBtn, composeBox);
                innerDiv.insertBefore(repliesDiv, composeBox);
            } else { innerDiv.appendChild(toggleBtn); innerDiv.appendChild(repliesDiv); }
        }
    } else {
        var tb = document.getElementById('toggle-' + rootCommentId);
        if (tb) { var c2 = repliesDiv.children.length + 1; tb.innerHTML = '&#9660; ' + c2 + (c2 === 1 ? ' reply' : ' replies'); }
    }
    repliesDiv.insertAdjacentHTML('beforeend', buildReplyHtml(reply, postId, rootCommentId));
    updateReplyBadge(postId, 1);
}

function toggleReplies(commentId, btn) {
    const list = document.getElementById('replies-' + commentId);
    if (!list) return;
    const isHidden = list.style.display === 'none';
    list.style.display = isHidden ? 'flex' : 'none';
    const count = list.children.length;
    btn.innerHTML = isHidden
        ? '&#9660; Hide ' + count + (count === 1 ? ' reply' : ' replies')
        : '&#9654; ' + count + (count === 1 ? ' reply' : ' replies');
}

async function deleteComment(commentId) {
    const res = await fetch('/Posts/DeleteComment', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ commentId })
    });
    if (res.ok) document.getElementById('comment-' + commentId)?.remove();
}

function updateToggleCount(commentId, delta) {
    const btn = document.getElementById('toggle-' + commentId);
    if (!btn) return;
    const list = document.getElementById('replies-' + commentId);
    const count = (list ? list.children.length : 0) + delta;
    btn.innerHTML = '&#9660; ' + count + (count === 1 ? ' reply' : ' replies');
}

function escHtml(str) {
    return (str || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

// commentId = this comment's own id = also its rootCommentId for replies
function buildCommentHtml(c, postId) {
    var pid = postId || c.postId || '';
    var initial = (c.authorName || '?')[0].toUpperCase();
    var photo = c.authorPhoto
        ? '<img class="post-avatar" src="' + c.authorPhoto + '" alt="" style="width:34px;height:34px;flex-shrink:0;" />'
        : '<div class="post-avatar-ph" style="width:34px;height:34px;font-size:.75rem;flex-shrink:0;">' + initial + '</div>';
    var authorBadge = c.authorUserId === authorUserId
        ? '<span style="font-size:.58rem;background:linear-gradient(90deg,#f26522,#e0541a);color:#fff;padding:.08rem .38rem;border-radius:50px;font-family:Nunito,sans-serif;font-weight:800;">Author</span>' : '';
    var delBtn = c.isOwnComment ? '<button class="comment-del" onclick="deleteComment(\'' + c.commentId + '\')">&#x2715;</button>' : '';
    // c.commentId is rootCommentId for all its replies
    var replies = (c.replies || []).map(function (r) { return buildReplyHtml(r, pid, c.commentId); }).join('');
    var toggleBtn = c.replies && c.replies.length > 0
        ? '<button class="toggle-replies-btn" id="toggle-' + c.commentId + '" onclick="toggleReplies(\'' + c.commentId + '\', this)">&#9660; ' + c.replies.length + (c.replies.length === 1 ? ' reply' : ' replies') + '</button>' : '';
    return '<div class="comment-item" id="comment-' + c.commentId + '">'
        + photo + '<div style="flex:1;min-width:0;">'
        + '<div class="comment-bubble">'
        + '<div class="comment-author">' + escHtml(c.authorName) + ' ' + authorBadge + '</div>'
        + '<div class="comment-text">' + escHtml(c.content) + '</div>'
        + '<div class="comment-foot"><span class="comment-time">Just now</span>'
        // rootCommentId = c.commentId (this top-level comment)
        + '<button class="comment-reply-btn" onclick="showReplyBox(\'' + c.commentId + '\',\'' + escHtml(c.authorName) + '\',\'' + c.commentId + '\',\'' + pid + '\')">&#x21a9; Reply</button>'
        + delBtn + '</div></div>'
        + toggleBtn + '<div class="replies-list" id="replies-' + c.commentId + '">' + replies + '</div>'
        + '<div class="reply-compose" id="reply-box-' + c.commentId + '" style="display:none;">'
        + '<input class="comment-input" id="reply-input-' + c.commentId + '" placeholder="Reply..." maxlength="500" />'
        + '<button class="comment-send-btn" onclick="submitReply(\'' + pid + '\',\'' + c.commentId + '\',\'' + c.commentId + '\')">&#x27A4;</button>'
        + '</div></div></div>';
}

// rootCommentId = always the top-level comment id — reply's Reply button passes this up
function buildReplyHtml(r, postId, rootCommentId) {
    var pid = postId || '';
    var rootId = rootCommentId || r.parentCommentId || '';
    var initial = (r.authorName || '?')[0].toUpperCase();
    var photo = r.authorPhoto
        ? '<img class="post-avatar" src="' + r.authorPhoto + '" alt="" style="width:28px;height:28px;flex-shrink:0;" />'
        : '<div class="post-avatar-ph" style="width:28px;height:28px;font-size:.65rem;flex-shrink:0;">' + initial + '</div>';
    var authorBadge = r.authorUserId === authorUserId
        ? '<span style="font-size:.58rem;background:linear-gradient(90deg,#f26522,#e0541a);color:#fff;padding:.08rem .38rem;border-radius:50px;font-family:Nunito,sans-serif;font-weight:800;">Author</span>' : '';
    var replyTag = r.replyToName ? '<span class="comment-reply-tag">&#x21a9; @' + escHtml(r.replyToName) + '</span>' : '';
    var delBtn = r.isOwnComment ? '<button class="comment-del" onclick="deleteComment(\'' + r.commentId + '\')">&#x2715;</button>' : '';
    return '<div class="reply-item" id="comment-' + r.commentId + '">'
        + photo + '<div style="flex:1;min-width:0;">'
        + '<div class="reply-bubble">'
        + '<div class="comment-author">' + escHtml(r.authorName) + ' ' + authorBadge + ' ' + replyTag + '</div>'
        + '<div class="comment-text">' + escHtml(r.content) + '</div>'
        + '<div class="comment-foot"><span class="comment-time">Just now</span>'
        // Pass rootId so any reply-to-reply still saves under the root comment
        + '<button class="comment-reply-btn" onclick="showReplyBox(\'' + r.commentId + '\',\'' + escHtml(r.authorName) + '\',\'' + rootId + '\',\'' + pid + '\')">&#x21a9; Reply</button>'
        + delBtn + '</div></div>'
        + '<div class="reply-compose" id="reply-box-' + r.commentId + '" style="display:none;">'
        + '<input class="comment-input" id="reply-input-' + r.commentId + '" placeholder="Reply..." maxlength="500" />'
        + '<button class="comment-send-btn" onclick="submitReply(\'' + pid + '\',\'' + rootId + '\',\'' + r.commentId + '\')">&#x27A4;</button>'
        + '</div></div></div>';
}

function buildPostCard(post) {
    var initial = (post.authorName || '?')[0].toUpperCase();
    var photo = post.authorPhoto
        ? '<img class="post-avatar" src="' + post.authorPhoto + '" alt="" />'
        : '<div class="post-avatar-ph">' + initial + '</div>';
    var authorBadge = post.authorUserId === authorUserId
        ? '<span style="font-size:.6rem;background:linear-gradient(90deg,#f26522,#e0541a);color:#fff;padding:.1rem .45rem;border-radius:50px;font-family:Nunito,sans-serif;font-weight:800;margin-left:.3rem;">Author</span>' : '';
    var delBtn = post.isOwnPost ? '<button class="post-delete-btn" onclick="deletePost(\'' + post.postId + '\')" title="Delete"><svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/></svg></button>' : '';
    var comments = (post.comments || []).map(function (c) { return buildCommentHtml(c, post.postId); }).join('');
    var replyCount = post.comments ? post.comments.length : 0;
    var badge = replyCount > 0 ? '<span class="reply-badge" style="font-size:.68rem;background:rgba(242,101,34,.12);color:#f26522;padding:.08rem .45rem;border-radius:50px;font-family:Nunito,sans-serif;font-weight:800;margin-left:.2rem;">' + replyCount + '</span>' : '';
    return '<div class="post-card" id="post-' + post.postId + '" data-post-id="' + post.postId + '">'
        + '<div class="post-header"><a href="/Posts/AuthorProfile?authorUserId=' + post.authorUserId + '" class="post-author-link">'
        + photo + '<div class="post-meta"><div class="post-author-name">' + escHtml(post.authorName) + authorBadge + '</div>'
        + '<div class="post-time">Just now</div></div></a>' + delBtn + '</div>'
        + '<div class="post-content">' + escHtml(post.content) + '</div>'
        + '<div class="post-actions">'
        + '<button class="post-action-btn" onclick="toggleLike(\'' + post.postId + '\', this)"><svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/></svg> <span class="like-count">0</span></button>'
        + '<button class="post-action-btn reply-count-btn" onclick="toggleComments(\'' + post.postId + '\', this)">&#x21a9; Reply' + badge + '</button>'
        + '</div>'
        + '<div class="comments-section" id="comments-' + post.postId + '" style="display:none;">'
        + '<div class="comment-list" id="comment-list-' + post.postId + '">' + comments + '</div>'
        + '</div>'
        + '<div style="display:none;gap:.5rem;align-items:center;margin-top:.5rem;" id="comment-input-row-' + post.postId + '">'
        + '<input class="comment-input" id="comment-input-' + post.postId + '" placeholder="Write a reply..." maxlength="500" />'
        + '<button class="comment-send-btn" onclick="submitComment(\'' + post.postId + '\')">&#x27A4;</button>'
        + '</div></div>';
}