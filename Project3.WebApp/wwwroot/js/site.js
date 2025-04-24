// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Force refresh of star ratings to prevent caching issues
document.addEventListener('DOMContentLoaded', function() {
    // Check if we came from a review edit (URL parameter 't' for timestamp)
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.has('t')) {
        console.log("Detected review update timestamp, refreshing star ratings");
        
        // Force browser to re-render all star ratings
        const allStars = document.querySelectorAll('.mini-stars');
        allStars.forEach(function(starContainer) {
            // Add a random class and remove it to force redraw
            const randomClass = 'refresh-' + Math.random().toString(36).substring(2, 15);
            starContainer.classList.add(randomClass);
            setTimeout(function() {
                starContainer.classList.remove(randomClass);
            }, 10);
        });
    }
});

// Function to update star ratings immediately when edit is submitted
function updateStarRatings(reviewId) {
    // This can be called after an AJAX update if you implement that
    const starContainers = document.querySelectorAll(`[data-review-id="${reviewId}"] .mini-stars`);
    starContainers.forEach(function(container) {
        container.setAttribute('data-timestamp', Date.now());
    });
}
