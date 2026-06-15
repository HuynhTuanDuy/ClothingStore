// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('DOMContentLoaded', function () {
    const ajaxForms = document.querySelectorAll('.ajax-add-to-cart');
    
    ajaxForms.forEach(form => {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const submitBtn = this.querySelector('button[type="submit"]');
            const originalText = submitBtn.innerHTML;
            submitBtn.innerHTML = 'Đang thêm...';
            submitBtn.disabled = true;

            const formData = new FormData(this);
            fetch(this.action, {
                method: this.method,
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    submitBtn.innerHTML = 'Đã thêm!';
                    const badge = document.getElementById('cart-badge');
                    if (badge) {
                        badge.textContent = data.totalItems;
                        badge.classList.remove('hidden');
                        badge.classList.add('scale-125');
                        setTimeout(() => badge.classList.remove('scale-125'), 300);
                    }
                    setTimeout(() => {
                        submitBtn.innerHTML = originalText;
                        submitBtn.disabled = false;
                    }, 2000);
                } else {
                    alert(data.message || 'Có lỗi xảy ra!');
                    submitBtn.innerHTML = originalText;
                    submitBtn.disabled = false;
                }
            })
            .catch(err => {
                console.error(err);
                alert('Lỗi kết nối mạng!');
                submitBtn.innerHTML = originalText;
                submitBtn.disabled = false;
            });
        });
    });
});
