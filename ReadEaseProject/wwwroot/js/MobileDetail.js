/* ── MobileDetail.js — Reviews, Comments, Replies ── */

/* ── Helpers ── */
function escH(s) {
    return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
function timeAgo(ts) {
    if (!ts) return '';
    const diff = Date.now() - ts;
    const m = Math.floor(diff / 60000);
    if (m < 1) return 'just now';
    if (m < 60) return m + 'm ago';
    const h = Math.floor(m / 60);
    if (h < 24) return h + 'h ago';
    const d = Math.floor(h / 24);
    if (d < 30) return d + 'd ago';
    return new Date(ts).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}
function avatarHtml(user, photo, size) {
    size = size || 32;
    if (photo) return '<img src="' + escH(photo) + '" alt="' + escH(user) + '" style="width:' + size + 'px;height:' + size + 'px;object-fit:cover;border-radius:50%;" />';
    var init = (user || '?')[0].toUpperCase();
    var colors = ['#f26522', '#6366f1', '#06b6d4', '#10b981', '#f59e0b', '#ec4899'];
    var color = colors[init.charCodeAt(0) % colors.length];
    return '<div style="width:' + size + 'px;height:' + size + 'px;border-radius:50%;background:' + color + ';color:#fff;font-size:' + Math.round(size * 0.38) + 'px;font-weight:800;display:flex;align-items:center;justify-content:center;flex-shrink:0;">' + init + '</div>';
}

/* ── Tab switching ── */
function switchTab(tab) {
    document.querySelectorAll('.det-tab').forEach(function (t) { t.classList.remove('active'); });
    document.querySelectorAll('.det-tab-content').forEach(function (c) { c.classList.remove('active'); });
    var tabEl = document.querySelector('[data-tab="' + tab + '"]');
    var contentEl = document.getElementById('tab-' + tab);
    if (tabEl) tabEl.classList.add('active');
    if (contentEl) contentEl.classList.add('active');
}

/* ── Description toggle ── */
function toggleDesc() {
    var s = document.getElementById('descShort');
    var f = document.getElementById('descFull');
    var btn = document.getElementById('descToggle');
    var expanded = f && f.style.display === 'inline';
    if (expanded) { f.style.display = 'none'; s.style.display = 'inline'; btn.textContent = 'Read more'; }
    else { if (s) s.style.display = 'none'; if (f) f.style.display = 'inline'; if (btn) btn.textContent = 'Show less'; }
}

/* ── Favorite ── */
async function toggleFav(bookId) {
    var btn = document.getElementById('favBtn');
    try {
        var res = await mobileApi('/Areas/Mobile/api/Library/favorite/' + bookId, { method: 'POST' });
        if (!res.ok) throw new Error();
        var d = await res.json();
        btn.textContent = d.isFavorite ? '\u2764\uFE0F' : '\uD83E\uDD0D';
        btn.classList.toggle('active', d.isFavorite);
        showToast(d.isFavorite ? 'Added to favorites \u2764\uFE0F' : 'Removed from favorites');
    } catch (e) { showToast('Something went wrong'); }
}

/* ── Follow ── */
async function toggleFollow(authorId) {
    var btn = document.getElementById('followBtn');
    try {
        var res = await mobileApi('/Areas/Mobile/api/Posts/toggle-follow', {
            method: 'POST',
            body: JSON.stringify({ followedId: authorId })
        });
        if (!res.ok) throw new Error();
        var d = await res.json();
        btn.textContent = d.isFollowing ? '\u2713 Following' : '+ Follow';
        btn.classList.toggle('following', d.isFollowing);
        showToast(d.isFollowing ? 'Now following author \u2713' : 'Unfollowed author');
    } catch (e) { showToast('Failed to follow/unfollow'); }
}

/* ── Star rating ── */
var currentRating = 0;
function setRating(rating) {
    currentRating = rating;
    document.querySelectorAll('#rateStars .star').forEach(function (s, i) { s.classList.toggle('active', i < rating); });
}

/* ── Submit Review ── */
async function submitReview(bookId) {
    if (currentRating === 0) { showToast('Please select a rating \u2B50'); return; }
    var text = document.getElementById('reviewText').value;
    try {
        var res = await mobileApi('/Areas/Mobile/api/Library/reviews', {
            method: 'POST',
            body: JSON.stringify({ bookId: bookId, stars: currentRating, text: text })
        });
        if (!res.ok) throw new Error();
        var data = await res.json();
        if (data.success) {
            showToast('Review submitted! \uD83C\uDF89');
            document.getElementById('reviewText').value = '';
            setRating(0);
            await loadReviews();
        } else {
            showToast(data.error || 'Failed to submit review');
        }
    } catch (e) { showToast('Something went wrong'); }
}

/* ── Like Review ── */
async function likeReview(reviewId) {
    try {
        var res = await mobileApi('/Areas/Mobile/api/Library/reviews/' + reviewId + '/like', { method: 'POST' });
        if (!res.ok) throw new Error();
        await loadReviews();
    } catch (e) { showToast('Failed to like review'); }
}

/* ── Author reply to review ── */
async function submitAuthorReply(reviewId) {
    var ta = document.getElementById('authorReplyTa-' + reviewId);
    var text = ta ? ta.value.trim() : '';
    if (!text) return;
    try {
        var res = await mobileApi('/Areas/Mobile/api/Library/reviews/' + reviewId + '/reply', {
            method: 'POST',
            body: JSON.stringify({ text: text })
        });
        if (!res.ok) throw new Error();
        showToast('Reply posted! \uD83D\uDCAC');
        await loadReviews();
    } catch (e) { showToast('Failed to post reply'); }
}
function toggleAuthorReplyForm(reviewId) {
    var form = document.getElementById('authorReplyForm-' + reviewId);
    if (form) form.classList.toggle('open');
}

/* ── Comments ── */
function toggleCommentForm(reviewId) {
    var wrap = document.getElementById('commentForm-' + reviewId);
    if (wrap) {
        wrap.classList.toggle('open');
        if (wrap.classList.contains('open')) {
            var ta = wrap.querySelector('textarea');
            if (ta) ta.focus();
        }
    }
}
async function submitComment(reviewId) {
    var ta = document.getElementById('commentTa-' + reviewId);
    var text = ta ? ta.value.trim() : '';
    if (!text) return;
    try {
        var res = await mobileApi('/Areas/Mobile/api/Library/comments', {
            method: 'POST',
            body: JSON.stringify({ reviewId: reviewId, text: text })
        });
        if (!res.ok) throw new Error();
        var data = await res.json();
        if (data.success) { ta.value = ''; showToast('Comment posted! \uD83D\uDCAC'); await loadReviews(); }
        else showToast(data.error || 'Failed to post comment');
    } catch (e) { showToast('Something went wrong'); }
}
async function likeComment(commentId) {
    try {
        var res = await mobileApi('/Areas/Mobile/api/Library/comments/' + commentId + '/like', { method: 'POST' });
        if (!res.ok) throw new Error();
        await loadReviews();
    } catch (e) { showToast('Failed to like comment'); }
}
function toggleReplyForm(commentId, reviewId, replyingTo) {
    var wrap = document.getElementById('replyForm-' + commentId);
    if (!wrap) return;
    wrap.classList.toggle('open');
    if (wrap.classList.contains('open')) {
        wrap.dataset.reviewId = reviewId;
        wrap.dataset.replyingTo = replyingTo || '';
        var ta = wrap.querySelector('textarea');
        if (ta) ta.focus();
    }
}
async function submitReply(commentId) {
    var wrap = document.getElementById('replyForm-' + commentId);
    var ta = wrap ? wrap.querySelector('textarea') : null;
    var text = ta ? ta.value.trim() : '';
    if (!text) return;
    var reviewId = wrap.dataset.reviewId;
    var replyingTo = wrap.dataset.replyingTo || '';
    try {
        var res = await mobileApi('/Areas/Mobile/api/Library/comments/' + commentId + '/reply', {
            method: 'POST',
            body: JSON.stringify({ reviewId: reviewId, text: text, replyingTo: replyingTo, parentReplyId: commentId })
        });
        if (!res.ok) throw new Error();
        var data = await res.json();
        if (data.success) { ta.value = ''; wrap.classList.remove('open'); showToast('Reply posted! \uD83D\uDCAC'); await loadReviews(); }
        else showToast(data.error || 'Failed to post reply');
    } catch (e) { showToast('Something went wrong'); }
}
function toggleComments(reviewId) {
    var block = document.getElementById('commentsBlock-' + reviewId);
    var btn = document.getElementById('toggleCommentsBtn-' + reviewId);
    if (!block) return;
    var hidden = block.style.display === 'none';
    block.style.display = hidden ? 'block' : 'none';
    if (btn) btn.textContent = hidden ? '\u25B2 Hide Comments' : '\u25BC Show Comments';
}

/* ── Build reply nodes (recursive) ── */
function buildReplyNodes(replies, reviewId) {
    if (!replies || !replies.length) return '';
    return replies.map(function (rep) {
        var replyingPill = rep.replyingTo
            ? '<div class="rv-replying-pill">\u21A9 @' + escH(rep.replyingTo) + '</div>'
            : '';
        var nestedReplies = (rep.replies && rep.replies.length)
            ? '<div class="rv-nested-replies">' + buildReplyNodes(rep.replies, reviewId) + '</div>'
            : '';
        return '<div class="rv-comment-item" style="margin-top:0.4rem;">' +
            '<div class="rv-comment-avatar">' + avatarHtml(rep.user, rep.userPhoto, 24) + '</div>' +
            '<div class="rv-comment-body">' +
            '<div class="rv-comment-bubble">' +
            replyingPill +
            '<div class="rv-comment-user">' + escH(rep.user) + '</div>' +
            '<div class="rv-comment-text">' + escH(rep.text) + '</div>' +
            '</div>' +
            '<div class="rv-comment-actions">' +
            '<button class="rv-comment-act ' + (rep.likedByMe ? 'liked' : '') + '" onclick="likeComment(\'' + rep.id + '\')">' +
            (rep.likedByMe ? '\u2764\uFE0F' : '\uD83E\uDD0D') + ' ' + rep.likeCount +
            '</button>' +
            '<button class="rv-comment-act" onclick="toggleReplyForm(\'' + rep.id + '\',\'' + reviewId + '\',\'' + escH(rep.user) + '\')">\u21A9 Reply</button>' +
            '<span class="rv-comment-time">' + timeAgo(rep.ts) + '</span>' +
            '</div>' +
            '<div class="rv-comment-input-wrap" id="replyForm-' + rep.id + '" data-review-id="' + reviewId + '" data-replying-to="' + escH(rep.user) + '">' +
            '<textarea class="rv-comment-input" placeholder="Reply to ' + escH(rep.user) + '..." rows="1"></textarea>' +
            '<button class="rv-comment-send" onclick="submitReply(\'' + rep.id + '\')">Send</button>' +
            '</div>' +
            nestedReplies +
            '</div>' +
            '</div>';
    }).join('');
}

/* ── Build full review card ── */
function buildReviewHtml(r) {
    var commentsCount = r.comments ? r.comments.length : 0;
    var commentsHtml = '';
    if (r.comments && r.comments.length) {
        commentsHtml = r.comments.map(function (c) {
            var nestedReplies = (c.replies && c.replies.length)
                ? '<div class="rv-nested-replies">' + buildReplyNodes(c.replies, r.id) + '</div>'
                : '';
            return '<div class="rv-comment-item">' +
                '<div class="rv-comment-avatar">' + avatarHtml(c.user, c.userPhoto, 28) + '</div>' +
                '<div class="rv-comment-body">' +
                '<div class="rv-comment-bubble">' +
                '<div class="rv-comment-user">' + escH(c.user) + '</div>' +
                '<div class="rv-comment-text">' + escH(c.text) + '</div>' +
                '</div>' +
                '<div class="rv-comment-actions">' +
                '<button class="rv-comment-act ' + (c.likedByMe ? 'liked' : '') + '" onclick="likeComment(\'' + c.id + '\')">' +
                (c.likedByMe ? '\u2764\uFE0F' : '\uD83E\uDD0D') + ' ' + c.likeCount +
                '</button>' +
                '<button class="rv-comment-act" onclick="toggleReplyForm(\'' + c.id + '\',\'' + r.id + '\',\'' + escH(c.user) + '\')">\u21A9 Reply</button>' +
                '<span class="rv-comment-time">' + timeAgo(c.ts) + '</span>' +
                '</div>' +
                '<div class="rv-comment-input-wrap" id="replyForm-' + c.id + '" data-review-id="' + r.id + '" data-replying-to="' + escH(c.user) + '">' +
                '<textarea class="rv-comment-input" placeholder="Reply to ' + escH(c.user) + '..." rows="1"></textarea>' +
                '<button class="rv-comment-send" onclick="submitReply(\'' + c.id + '\')">Send</button>' +
                '</div>' +
                nestedReplies +
                '</div>' +
                '</div>';
        }).join('');
    }

    var authorReplyHtml = '';
    if (r.authorReply) {
        var replyTime = r.authorRepliedTs
            ? '<span style="font-size:0.62rem;color:var(--re-text-muted,#9994a8);margin-left:auto;">' + timeAgo(r.authorRepliedTs) + '</span>'
            : '';
        authorReplyHtml =
            '<div class="rv-author-reply-card">' +
            '<div class="rv-author-reply-header">' +
            '<span style="font-size:0.75rem;font-weight:800;color:var(--re-text,#e8e6f0);">Author</span>' +
            '<span class="rv-author-badge">\u270D\uFE0F Reply</span>' +
            replyTime +
            '</div>' +
            '<div class="rv-author-reply-text">' + escH(r.authorReply) + '</div>' +
            '</div>';
    } else if (window.detailIsBookAuthor) {
        authorReplyHtml =
            '<button class="rv-add-comment-btn" onclick="toggleAuthorReplyForm(\'' + r.id + '\')">\uD83D\uDCAC Reply to this review</button>' +
            '<div class="rv-comment-input-wrap" id="authorReplyForm-' + r.id + '">' +
            '<textarea class="rv-comment-input" id="authorReplyTa-' + r.id + '" placeholder="Write your reply as the author..." rows="2"></textarea>' +
            '<button class="rv-comment-send" onclick="submitAuthorReply(\'' + r.id + '\')">Post</button>' +
            '</div>';
    }

    var starsHtml = '\u2605'.repeat(r.stars) + '\u2606'.repeat(5 - r.stars);
    var reviewText = r.text ? '<div class="det-review-text">' + escH(r.text) + '</div>' : '';
    var showCommentsBtn = commentsCount > 0
        ? '<button id="toggleCommentsBtn-' + r.id + '" class="rv-show-comments-btn" onclick="toggleComments(\'' + r.id + '\')">\u25BC ' + commentsCount + ' Comment' + (commentsCount !== 1 ? 's' : '') + '</button>'
        : '';
    var commentsBlock = commentsCount > 0
        ? '<div class="rv-comment-block" id="commentsBlock-' + r.id + '" style="display:none;">' + commentsHtml + '</div>'
        : '';

    return '<div class="det-review-item" id="review-' + r.id + '">' +
        '<div class="det-review-header">' +
        '<div class="det-review-avatar">' + avatarHtml(r.user, r.userPhoto, 36) + '</div>' +
        '<div class="det-review-meta">' +
        '<div class="det-review-user">' + escH(r.user) + '</div>' +
        '<div class="det-review-date">' + timeAgo(r.ts) + '</div>' +
        '</div>' +
        '<div class="det-review-stars">' + starsHtml + '</div>' +
        '</div>' +
        reviewText +
        '<div class="det-review-actions">' +
        '<button class="' + (r.likedByMe ? 'liked' : '') + '" onclick="likeReview(\'' + r.id + '\')">' +
        (r.likedByMe ? '\u2764\uFE0F' : '\uD83E\uDD0D') + ' ' + r.likeCount +
        '</button>' +
        '<button onclick="toggleCommentForm(\'' + r.id + '\')">\uD83D\uDCAC Comment</button>' +
        showCommentsBtn +
        '</div>' +
        authorReplyHtml +
        '<div class="rv-comment-input-wrap" id="commentForm-' + r.id + '">' +
        '<textarea class="rv-comment-input" id="commentTa-' + r.id + '" placeholder="Write a comment..." rows="1"></textarea>' +
        '<button class="rv-comment-send" onclick="submitComment(\'' + r.id + '\')">Send</button>' +
        '</div>' +
        commentsBlock +
        '</div>';
}

/* ── Load & render reviews ── */
var allReviews = [];
async function loadReviews() {
    try {
        var res = await mobileApi('/Areas/Mobile/api/Library/reviews/' + window.detailBookId);
        if (!res.ok) return;
        var d = await res.json();
        allReviews = d.reviews || [];

        // Update rating summary
        if (d.average !== undefined) {
            var avg = parseFloat(d.average).toFixed(1);
            var avgEl = document.getElementById('rvAvgDisplay');
            var cntEl = document.getElementById('rvCountDisplay');
            var starsEl = document.getElementById('rvStarsDisplay');
            if (avgEl) avgEl.textContent = avg;
            if (cntEl) cntEl.textContent = (d.count || 0) + ' ratings';
            if (starsEl) {
                var rounded = Math.round(parseFloat(d.average));
                var html = '';
                for (var i = 0; i < 5; i++) html += '<span>' + (i < rounded ? '\u2605' : '\u2606') + '</span>';
                starsEl.innerHTML = html;
            }
        }

        var list = document.getElementById('reviewsList');
        if (!allReviews.length) {
            list.innerHTML = '<div class="mob-empty" style="padding:1rem 0;">' +
                '<span class="mob-empty-icon">\u2B50</span>' +
                '<div class="mob-empty-title">No reviews yet</div>' +
                '<div class="mob-empty-sub">Be the first to share your thoughts!</div>' +
                '</div>';
            return;
        }
        list.innerHTML = allReviews.map(buildReviewHtml).join('');

        if (!window.detailIsBookAuthor && d.myStars > 0) setRating(d.myStars);
    } catch (e) { console.error('loadReviews error:', e); }
}

/* ── Similar Books ── */
async function loadSimilar() {
    try {
        var res = await mobileApi('/Areas/Mobile/api/Library/similar/' + window.detailBookId + '?limit=6');
        if (!res.ok) return;
        var books = await res.json();
        if (!books.length) return;
        var rail = document.getElementById('similarRail');
        var section = document.getElementById('similarSection');
        rail.innerHTML = books.map(function (b) {
            var cover = b.coverImage
                ? '<img class="bk-cover" src="' + escH(b.coverImage) + '" alt="' + escH(b.title) + '" loading="lazy" />'
                : '<div class="bk-cover-ph">\uD83D\uDCD6</div>';
            return '<a class="bk-card" href="/Mobile/Detail/' + b.id + '">' +
                '<div class="bk-cover-wrap">' + cover +
                '<span class="bk-badge-pill ' + (b.isFree ? 'bk-badge-free' : 'bk-badge-pro') + '">' +
                (b.isFree ? 'FREE' : 'PRO') +
                '</span>' +
                '</div>' +
                '<div class="bk-info">' +
                '<div class="bk-title">' + escH(b.title) + '</div>' +
                '<div class="bk-author">' + escH(b.author) + '</div>' +
                '</div></a>';
        }).join('');
        section.style.display = 'block';
    } catch (e) { }
}
