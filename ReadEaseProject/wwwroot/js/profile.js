// profile.js
// myUserId and activeTab are set inline in Profile.cshtml

document.addEventListener('DOMContentLoaded', function () {
    if (typeof activeTab !== 'undefined' && activeTab === 'password') {
        var btn = document.querySelector(".pf-tab[onclick*=\"'password'\"]");
        if (btn) switchTab('password', btn);
    }
    document.getElementById('myPostInput')?.addEventListener('input', function () {
        document.getElementById('myPostCharCount').textContent = this.value.length;
    });
});

function switchTab(name, btn) {
    document.querySelectorAll('.pf-panel').forEach(function (p) { p.classList.remove('active'); });
    document.querySelectorAll('.pf-tab').forEach(function (b) { b.classList.remove('active'); });
    document.getElementById('tab-' + name).classList.add('active');
    btn.classList.add('active');
    if (name === 'conversations' && !window._postsLoaded) { loadMyPosts(); window._postsLoaded = true; }
}

function previewCover(input) {
    if (!input.files || !input.files[0]) return;
    var reader = new FileReader();
    reader.onload = function (e) {
        var hero = document.querySelector('.pf-hero');
        hero.classList.add('has-cover');
        var img = hero.querySelector('.pf-hero-bg');
        if (!img) { img = document.createElement('img'); img.className = 'pf-hero-bg'; hero.insertBefore(img, hero.firstChild); }
        img.src = e.target.result;
    };
    reader.readAsDataURL(input.files[0]);
}

var eyeOpen = "<svg xmlns='http://www.w3.org/2000/svg' width='17' height='17' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z'/><circle cx='12' cy='12' r='3'/></svg>";
var eyeClosed = "<svg xmlns='http://www.w3.org/2000/svg' width='17' height='17' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24'/><line x1='1' y1='1' x2='23' y2='23'/></svg>";

function togglePw(inputId, btn) {
    var input = document.getElementById(inputId);
    var isHidden = input.type === 'password';
    input.type = isHidden ? 'text' : 'password';
    btn.innerHTML = isHidden ? eyeClosed : eyeOpen;
    btn.style.color = isHidden ? '#f26522' : '#c4bfb8';
}

async function loadMyPosts() {
    try {
        var res = await fetch('/Posts/GetMyPosts');
        if (!res.ok) return;
        var posts = await res.json();
        var list = document.getElementById('myPostList');
        if (!posts || posts.length === 0) { document.getElementById('myPostsEmpty').style.display = 'block'; return; }
        posts.forEach(function (p) { list.insertAdjacentHTML('beforeend', buildPostCard(p)); });
    } catch (e) { console.error(e); }
}

async function submitMyPost() {
    var input = document.getElementById('myPostInput');
    var content = input.value.trim();
    if (!content) return;
    var btn = document.getElementById('myPostBtn');
    btn.disabled = true;
    try {
        var res = await fetch('/Posts/CreatePost', {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ content, wallOwnerId: myUserId })
        });
        if (!res.ok) return;
        var post = await res.json();
        input.value = '';
        document.getElementById('myPostCharCount').textContent = '0';
        document.getElementById('myPostsEmpty').style.display = 'none';
        document.getElementById('myPostList').insertAdjacentHTML('afterbegin', buildPostCard(post));
    } finally { btn.disabled = false; }
}

async function deletePost(postId) {
    if (!confirm('Delete this post?')) return;
    var res = await fetch('/Posts/DeletePost', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ postId })
    });
    if (res.ok) document.getElementById('post-' + postId)?.remove();
}

async function toggleLike(postId, btn) {
    var res = await fetch('/Posts/ToggleLike', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ postId })
    });
    var data = await res.json();
    btn.classList.toggle('liked', data.isLiked);
    btn.innerHTML = (data.isLiked ? '&#x2764;&#xFE0F;' : '&#x1F90D;') + ' <span class="like-count">' + data.likeCount + '</span>';
}

function toggleComments(postId) {
    var section = document.getElementById('comments-' + postId);
    var inputRow = document.getElementById('comment-input-row-' + postId);
    var isHidden = section.style.display === 'none';
    section.style.display = isHidden ? 'block' : 'none';
    if (inputRow) inputRow.style.display = isHidden ? 'flex' : 'none';
    if (isHidden) { var inp = document.getElementById('comment-input-' + postId); if (inp) inp.focus(); }
}

async function submitComment(postId) {
    var input = document.getElementById('comment-input-' + postId);
    var content = input.value.trim();
    if (!content) return;
    var res = await fetch('/Posts/AddComment', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ postId, content })
    });
    if (!res.ok) return;
    var comment = await res.json();
    input.value = '';
    document.getElementById('comment-list-' + postId).insertAdjacentHTML('beforeend', buildCommentHtml(comment, postId));
    updateReplyBadge(postId, 1);
}

// commentId = which reply box to show, rootCommentId = top-level comment to save under
function showReplyBox(commentId, replyToName, rootCommentId, postId) {
    document.querySelectorAll('[id^="reply-box-"]').forEach(function (el) {
        if (el.id !== 'reply-box-' + commentId) el.style.display = 'none';
    });
    var box = document.getElementById('reply-box-' + commentId);
    var isHidden = box.style.display === 'none';
    box.style.display = isHidden ? 'flex' : 'none';
    if (isHidden) {
        var inp = document.getElementById('reply-input-' + commentId);
        inp.placeholder = 'Reply to ' + replyToName + '...';
        inp.dataset.replyToName = replyToName;
        // Update send button to always save under rootCommentId
        var sendBtn = inp.nextElementSibling;
        if (sendBtn) sendBtn.setAttribute('onclick', 'submitReply("' + (postId || '') + '","' + (rootCommentId || commentId) + '","' + commentId + '")');
        inp.focus();
    }
}

// postId = post, rootCommentId = save under this in DB, displayParentId = read input from this
async function submitReply(postId, rootCommentId, displayParentId) {
    var input = document.getElementById('reply-input-' + displayParentId);
    var content = input.value.trim();
    if (!content) return;
    var replyToName = input.dataset.replyToName || '';
    var res = await fetch('/Posts/AddComment', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ postId, content, parentCommentId: rootCommentId, replyToName })
    });
    if (!res.ok) return;
    var reply = await res.json();
    input.value = '';
    document.getElementById('reply-box-' + displayParentId).style.display = 'none';

    // Always insert visually under rootCommentId
    var repliesDiv = document.getElementById('replies-' + rootCommentId);
    if (!repliesDiv) {
        var parentEl = document.getElementById('comment-' + rootCommentId);
        var innerDiv = parentEl ? (parentEl.querySelector('div[style*="flex:1"]') || parentEl.querySelector('.comment-inner') || parentEl.lastElementChild) : null;
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
    var list = document.getElementById('replies-' + commentId);
    if (!list) return;
    var isHidden = list.style.display === 'none';
    list.style.display = isHidden ? 'flex' : 'none';
    var count = list.children.length;
    btn.innerHTML = isHidden
        ? '&#9660; Hide ' + count + (count === 1 ? ' reply' : ' replies')
        : '&#9654; ' + count + (count === 1 ? ' reply' : ' replies');
}

async function deleteComment(commentId) {
    var res = await fetch('/Posts/DeleteComment', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ commentId })
    });
    if (res.ok) document.getElementById('comment-' + commentId)?.remove();
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
        badge.style.cssText = 'font-size:.68rem;background:rgba(242,101,34,.12);color:#f26522;padding:.08rem .45rem;border-radius:50px;font-family:Nunito,sans-serif;font-weight:800;margin-left:.3rem;';
        btn.appendChild(badge);
    }
    badge.textContent = (parseInt(badge.textContent) || 0) + delta;
}

function escHtml(str) {
    return (str || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

function formatDate(d) {
    if (!d) return 'Just now';
    var date = new Date(d);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }) + ' · ' + date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
}

// c.commentId is the rootCommentId for all replies under this comment
function buildCommentHtml(c, postId) {
    var pid = postId || '';
    var initial = (c.authorName || '?')[0].toUpperCase();
    var photo = c.authorPhoto
        ? '<img class="post-avatar" src="' + c.authorPhoto + '" alt="" style="width:34px;height:34px;flex-shrink:0;" />'
        : '<div class="post-avatar-ph" style="width:34px;height:34px;font-size:.75rem;flex-shrink:0;">' + initial + '</div>';
    var delBtn = c.isOwnComment ? '<button class="comment-del" onclick="deleteComment(\'' + c.commentId + '\')">&#x2715;</button>' : '';
    var replyBtn = '<button class="comment-reply-btn" onclick="showReplyBox(\'' + c.commentId + '\',\'' + escHtml(c.authorName) + '\',\'' + c.commentId + '\',\'' + pid + '\')">&#x21a9; Reply</button>';
    var replies = (c.replies || []).map(function (r) { return buildReplyHtml(r, pid, c.commentId); }).join('');
    var toggleBtn = c.replies && c.replies.length > 0
        ? '<button class="toggle-replies-btn" id="toggle-' + c.commentId + '" onclick="toggleReplies(\'' + c.commentId + '\', this)">&#9660; ' + c.replies.length + (c.replies.length === 1 ? ' reply' : ' replies') + '</button>' : '';
    return '<div class="comment-item" id="comment-' + c.commentId + '">'
        + photo + '<div class="comment-inner" style="flex:1;min-width:0;">'
        + '<div class="comment-bubble">'
        + '<div class="comment-author">' + escHtml(c.authorName) + '</div>'
        + '<div class="comment-text">' + escHtml(c.content) + '</div>'
        + '<div class="comment-foot"><span class="comment-time">' + formatDate(c.createdAt) + '</span>' + replyBtn + delBtn + '</div>'
        + '</div>'
        + toggleBtn + '<div class="replies-list" id="replies-' + c.commentId + '">' + replies + '</div>'
        + '<div class="reply-compose" id="reply-box-' + c.commentId + '" style="display:none;">'
        + '<input class="comment-input" id="reply-input-' + c.commentId + '" placeholder="Reply..." maxlength="500" />'
        + '<button class="comment-send-btn" onclick="submitReply(\'' + pid + '\',\'' + c.commentId + '\',\'' + c.commentId + '\')">&#x27A4;</button>'
        + '</div></div></div>';
}

// rootCommentId always passed through so deep replies save flat under root
function buildReplyHtml(r, postId, rootCommentId) {
    var pid = postId || '';
    var rootId = rootCommentId || r.parentCommentId || '';
    var initial = (r.authorName || '?')[0].toUpperCase();
    var photo = r.authorPhoto
        ? '<img class="post-avatar" src="' + r.authorPhoto + '" alt="" style="width:28px;height:28px;flex-shrink:0;" />'
        : '<div class="post-avatar-ph" style="width:28px;height:28px;font-size:.65rem;flex-shrink:0;">' + initial + '</div>';
    var replyTag = r.replyToName ? '<span class="comment-reply-tag">&#x21a9; @' + escHtml(r.replyToName) + '</span>' : '';
    var delBtn = r.isOwnComment ? '<button class="comment-del" onclick="deleteComment(\'' + r.commentId + '\')">&#x2715;</button>' : '';
    return '<div class="reply-item" id="comment-' + r.commentId + '">'
        + photo + '<div style="flex:1;min-width:0;">'
        + '<div class="reply-bubble">'
        + '<div class="comment-author">' + escHtml(r.authorName) + ' ' + replyTag + '</div>'
        + '<div class="comment-text">' + escHtml(r.content) + '</div>'
        + '<div class="comment-foot"><span class="comment-time">' + formatDate(r.createdAt) + '</span>'
        // Pass rootId so replying to a reply still saves under root
        + '<button class="comment-reply-btn" onclick="showReplyBox(\'' + r.commentId + '\',\'' + escHtml(r.authorName) + '\',\'' + rootId + '\',\'' + pid + '\')">&#x21a9; Reply</button>'
        + delBtn + '</div></div>'
        + '<div class="reply-compose" id="reply-box-' + r.commentId + '" style="display:none;">'
        + '<input class="comment-input" id="reply-input-' + r.commentId + '" placeholder="Reply..." maxlength="500" />'
        + '<button class="comment-send-btn" onclick="submitReply(\'' + pid + '\',\'' + rootId + '\',\'' + r.commentId + '\')">&#x27A4;</button>'
        + '</div></div></div>';
}

function buildPostCard(post) {
    var initial = (post.authorName || '?')[0].toUpperCase();
    var photo = post.authorPhoto ? '<img class="post-avatar" src="' + post.authorPhoto + '" alt="" />' : '<div class="post-avatar-ph">' + initial + '</div>';
    var delBtn = post.isOwnPost ? '<button class="post-delete-btn" onclick="deletePost(\'' + post.postId + '\')" title="Delete">&#x1F5D1;</button>' : '';
    var comments = (post.comments || []).map(function (c) { return buildCommentHtml(c, post.postId); }).join('');
    var replyCount = post.comments ? post.comments.length : 0;
    var badge = replyCount > 0 ? '<span class="reply-badge" style="font-size:.68rem;background:rgba(242,101,34,.12);color:#f26522;padding:.08rem .45rem;border-radius:50px;font-family:Nunito,sans-serif;font-weight:800;margin-left:.3rem;">' + replyCount + '</span>' : '';
    return '<div class="post-card" id="post-' + post.postId + '">'
        + '<div class="post-header"><span class="post-author-link" style="cursor:default;">' + photo
        + '<div class="post-meta"><div class="post-author-name">' + escHtml(post.authorName) + '</div>'
        + '<div class="post-time">' + formatDate(post.createdAt) + '</div></div></span>' + delBtn + '</div>'
        + '<div class="post-content">' + escHtml(post.content) + '</div>'
        + '<div class="post-actions">'
        + '<button class="post-action-btn ' + (post.isLikedByCurrentUser ? 'liked' : '') + '" onclick="toggleLike(\'' + post.postId + '\', this)">'
        + (post.isLikedByCurrentUser ? '&#x2764;&#xFE0F;' : '&#x1F90D;') + ' <span class="like-count">' + (post.likeCount || 0) + '</span></button>'
        + '<button class="post-action-btn reply-count-btn" onclick="toggleComments(\'' + post.postId + '\')">&#x21a9; Reply' + badge + '</button>'
        + '</div>'
        + '<div class="comments-section" id="comments-' + post.postId + '" style="display:none;">'
        + '<div class="comment-list" id="comment-list-' + post.postId + '">' + comments + '</div>'
        + '</div>'
        + '<div style="display:none;gap:.5rem;align-items:center;margin-top:.5rem;" id="comment-input-row-' + post.postId + '">'
        + '<input class="comment-input" id="comment-input-' + post.postId + '" placeholder="Write a reply..." maxlength="500" />'
        + '<button class="comment-send-btn" onclick="submitComment(\'' + post.postId + '\')">&#x27A4;</button>'
        + '</div></div>';
}