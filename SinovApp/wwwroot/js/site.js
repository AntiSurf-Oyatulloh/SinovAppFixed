// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Toast notification
function showToast(message, type = 'success')
{
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type} border-0 position-fixed bottom-0 end-0 m-3`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');

    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    document.body.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();

    toast.addEventListener('hidden.bs.toast', () =>
    {
        document.body.removeChild(toast);
    });
}

// Loading spinner
function showLoading(element)
{
    const spinner = document.createElement('div');
    spinner.className = 'loading';
    element.appendChild(spinner);
    return spinner;
}

function hideLoading(spinner)
{
    if (spinner && spinner.parentNode)
    {
        spinner.parentNode.removeChild(spinner);
    }
}

// Form validation
function validateForm(form)
{
    let isValid = true;
    const inputs = form.querySelectorAll('input[required], select[required], textarea[required]');

    inputs.forEach(input =>
    {
        if (!input.value.trim())
        {
            isValid = false;
            input.classList.add('is-invalid');
        } else
        {
            input.classList.remove('is-invalid');
        }
    });

    return isValid;
}

// File upload preview
function previewFile(input, previewElement)
{
    if (input.files && input.files[0])
    {
        const reader = new FileReader();
        reader.onload = function (e)
        {
            previewElement.src = e.target.result;
        };
        reader.readAsDataURL(input.files[0]);
    }
}

// Confirm dialog
function confirmAction(message)
{
    return new Promise((resolve) =>
    {
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.setAttribute('tabindex', '-1');

        modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Tasdiqlash</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <p>${message}</p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Bekor qilish</button>
                        <button type="button" class="btn btn-primary" id="confirmBtn">Tasdiqlash</button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(modal);
        const bsModal = new bootstrap.Modal(modal);

        modal.querySelector('#confirmBtn').addEventListener('click', () =>
        {
            bsModal.hide();
            resolve(true);
        });

        modal.addEventListener('hidden.bs.modal', () =>
        {
            document.body.removeChild(modal);
            resolve(false);
        });

        bsModal.show();
    });
}

// Initialize tooltips
document.addEventListener('DOMContentLoaded', function ()
{
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl)
    {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});

// Kitob faylini modalda ko'rish logikasi
document.addEventListener('DOMContentLoaded', function ()
{
    const previewModal = document.getElementById('previewModal');
    const bookPreviewIframe = document.getElementById('bookPreviewIframe');
    const previewError = document.getElementById('previewError');
    const previewLoading = document.getElementById('previewLoading');

    if (previewModal)
    {
        previewModal.addEventListener('show.bs.modal', function (event)
        {
            // Moddalni ochuvchi tugma
            const button = event.relatedTarget;
            // data-file atributidan fayl nomini olish (endi ID olinadi)
            const bookId = button.getAttribute('data-id');

            // Oldingi kontentni tozalash va yuklanishni ko'rsatish
            bookPreviewIframe.src = '';
            previewError.classList.add('d-none');
            previewLoading.classList.remove('d-none');

            // Kitob IDsi mavjudligini tekshirish
            if (!bookId) // !fileName o'rniga bookId tekshiriladi
            {
                previewLoading.classList.add('d-none');
                previewError.classList.remove('d-none');
                previewError.textContent = "Kitob ID topilmadi."; // Xabar o'zgartirildi
                console.error('Preview load error: Book ID not found.'); // Log o'zgartirildi
                return;
            }

            // Fayl kengaytmasini olish (Preview action ichida aniqlanadi, bu yerda shart emas)
            // const fileExtension = fileName.split('.').pop().toLowerCase();

            // Agar fayl PDF bo'lsa, uni iframe orqali ko'rsatishga urinish (Endi har qanday fayl uchun Preview action chaqiriladi)
            // if (fileExtension === 'pdf')
            // {
            // Serverdagi ko'rish URL manzilini yaratish (Preview actionini chaqirish)
            const previewUrl = `/Books/Preview?id=${bookId}`; // URL o'zgartirildi

            bookPreviewIframe.src = previewUrl;

            // Iframe yuklanishini kuzatish
            bookPreviewIframe.onload = function ()
            {
                previewLoading.classList.add('d-none');
                previewError.classList.add('d-none'); // Success holatida ham errorni yashiramiz
            };

            bookPreviewIframe.onerror = function ()
            {
                previewLoading.classList.add('d-none');
                previewError.classList.remove('d-none');
                previewError.textContent = "Faylni yuklashda xatolik yuz berdi.";
                console.error('Preview iframe load error.');
            };

            // } else // Bu qism endi kerak emas
            // {
            //     // PDF bo'lmagan fayllar uchun xabar ko'rsatish
            //     previewLoading.classList.add('d-none');
            //     previewError.classList.remove('d-none');
            //     previewError.textContent = "Faylni ko'rish uchun mos formatda emas. Iltimos, yuklab oling.";
            //     console.warn('Preview not supported for file type:', fileExtension);
            // }
        });

        // Modal yopilganda iframe src ni tozalash
        previewModal.addEventListener('hidden.bs.modal', function ()
        {
            bookPreviewIframe.src = '';
            previewError.classList.add('d-none');
            previewLoading.classList.add('d-none');
        });
    }
});
