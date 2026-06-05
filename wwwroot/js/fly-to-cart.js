document.addEventListener("DOMContentLoaded", function () {
    const addToCartForms = document.querySelectorAll("form[action='/Cart/Add']");
    const cartAnchor = document.getElementById("cart-anchor");
    const cartBadge = document.getElementById("cart-badge");
    const cartIconSvg = document.getElementById("cart-icon-svg");

    addToCartForms.forEach(form => {
        form.addEventListener("submit", async function (e) {
            e.preventDefault(); // Prevent standard postback

            const submitBtn = form.querySelector("button[type='submit']");
            if (submitBtn) {
                // SPAM PREVENTION: Disable button immediately
                submitBtn.disabled = true;
                submitBtn.dataset.originalText = submitBtn.innerHTML;
                submitBtn.innerHTML = "Adding...";
                submitBtn.style.opacity = "0.7";
                submitBtn.style.cursor = "not-allowed";
            }

            try {
                const formData = new FormData(form);
                
                // AJAX Request with reliable detection headers
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest',
                        'Accept': 'application/json'
                    }
                });

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                const result = await response.json();

                if (result.success) {
                    // Update Cart Badge directly without animation
                    updateCartBadge(result.totalItems);
                    resetButton(submitBtn);
                } else {
                    // Show error toast/alert
                    alert(result.message || "Failed to add to cart.");
                    resetButton(submitBtn);
                }

            } catch (error) {
                console.error("Error adding to cart:", error);
                alert("An error occurred. Please try again.");
                resetButton(submitBtn);
            }
        });
    });



    function updateCartBadge(count) {
        if (!cartBadge) return;
        
        cartBadge.innerText = count;
        cartBadge.style.display = "block";
        
        // Bounce animation
        cartBadge.style.transform = "scale(1.5)";
        setTimeout(() => {
            cartBadge.style.transform = "scale(1)";
        }, 300);
    }

    function resetButton(btn) {
        if (!btn) return;
        btn.disabled = false;
        btn.innerHTML = btn.dataset.originalText || "Add to Cart";
        btn.style.opacity = "1";
        btn.style.cursor = "pointer";
    }
});
