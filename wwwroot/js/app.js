// Lundin Assets — Client-side JS helpers

// Auto-dismiss toast messages after 4 seconds
document.addEventListener('DOMContentLoaded', () => {
    setupToastAutoDismiss();
});

// Called by Blazor after navigation
window.setupToastAutoDismiss = () => {
    document.querySelectorAll('.toast.show').forEach(toast => {
        if (!toast.dataset.autoDismiss) {
            toast.dataset.autoDismiss = 'true';
            setTimeout(() => {
                toast.classList.remove('show');
            }, 4000);
        }
    });
};

// Submit a form by id — used by Register to auto-login after account creation
window.submitForm = (id) => document.getElementById(id)?.submit();

