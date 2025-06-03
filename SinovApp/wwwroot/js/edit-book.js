// Image preview functionality
function setupImagePreview()
{
    const imageInput = document.getElementById('UploadedImage');
    const previewContainer = imageInput.parentElement;

    imageInput.addEventListener('change', function (e)
    {
        const file = e.target.files[0];
        if (file)
        {
            // Validate file type
            if (!file.type.startsWith('image/'))
            {
                showToast('Faqat rasm fayllari yuklanishi mumkin', 'danger');
                imageInput.value = '';
                return;
            }

            // Validate file size (max 5MB)
            if (file.size > 5 * 1024 * 1024)
            {
                showToast('Rasm hajmi 5MB dan oshmasligi kerak', 'danger');
                imageInput.value = '';
                return;
            }

            const reader = new FileReader();
            reader.onload = function (e)
            {
                const preview = document.createElement('img');
                preview.src = e.target.result;
                preview.className = 'img-thumbnail mt-2';
                preview.style.maxHeight = '200px';

                const existingPreview = previewContainer.querySelector('img');
                if (existingPreview)
                {
                    previewContainer.removeChild(existingPreview);
                }
                previewContainer.appendChild(preview);
            };
            reader.readAsDataURL(file);
        }
    });
}

// File validation
function validateFile(input)
{
    const file = input.files[0];
    if (file)
    {
        // Validate file type
        if (!file.name.toLowerCase().endsWith('.pdf'))
        {
            showToast('Faqat PDF fayllar yuklanishi mumkin', 'danger');
            input.value = '';
            return false;
        }

        // Validate file size (max 50MB)
        if (file.size > 50 * 1024 * 1024)
        {
            showToast('Fayl hajmi 50MB dan oshmasligi kerak', 'danger');
            input.value = '';
            return false;
        }

        return true;
    }
    return true;
}

// Form submission handling
function setupFormSubmission()
{
    const form = document.querySelector('form');
    const submitButton = form.querySelector('button[type="submit"]');
    const originalButtonText = submitButton.innerHTML;

    form.addEventListener('submit', async function (e)
    {
        e.preventDefault();

        // Validate form
        if (!validateForm(form))
        {
            showToast('Iltimos, barcha majburiy maydonlarni to\'ldiring', 'danger');
            return;
        }

        // Validate files
        const fileInput = document.getElementById('UploadedFile');
        if (fileInput.files.length > 0 && !validateFile(fileInput))
        {
            return;
        }

        try
        {
            // Show loading state
            submitButton.disabled = true;
            submitButton.innerHTML = `
                <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                Saqlanmoqda...
            `;

            // Create FormData
            const formData = new FormData(form);

            // Submit form
            const response = await fetch(form.action, {
                method: 'POST',
                body: formData
            });

            if (response.ok)
            {
                showToast('Kitob muvaffaqiyatli yangilandi!', 'success');
                // Redirect after short delay
                setTimeout(() =>
                {
                    window.location.href = '/Admin/Index';
                }, 1500);
            } else
            {
                throw new Error('Server xatosi');
            }
        } catch (error)
        {
            showToast('Xatolik yuz berdi. Iltimos, qaytadan urinib ko\'ring', 'danger');
            console.error('Form submission error:', error);
        } finally
        {
            // Reset button state
            submitButton.disabled = false;
            submitButton.innerHTML = originalButtonText;
        }
    });
}

// Progress indicators
function showProgress(message)
{
    const progressDiv = document.createElement('div');
    progressDiv.className = 'progress-indicator alert alert-info alert-dismissible fade show';
    progressDiv.innerHTML = `
        <div class="d-flex align-items-center">
            <div class="spinner-border spinner-border-sm me-2" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    document.querySelector('.card-body').prepend(progressDiv);
}

function hideProgress()
{
    const progressDiv = document.querySelector('.progress-indicator');
    if (progressDiv)
    {
        progressDiv.remove();
    }
}

// Initialize all functionality
document.addEventListener('DOMContentLoaded', function ()
{
    setupImagePreview();
    setupFormSubmission();

    // Add file validation listeners
    const fileInput = document.getElementById('UploadedFile');
    if (fileInput)
    {
        fileInput.addEventListener('change', function ()
        {
            validateFile(this);
        });
    }
}); 