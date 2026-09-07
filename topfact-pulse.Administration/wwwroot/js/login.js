const initializeLogin = () => {
    const form = document.getElementById('login-form');
    const togglePasswordButton = document.getElementById('togglePasswordButton');

    if (form instanceof HTMLFormElement) {
        form.addEventListener('submit', handleLoginSubmit);
    }

    if (togglePasswordButton instanceof HTMLButtonElement) {
        togglePasswordButton.addEventListener('click', togglePasswordVisibility);
    }
};

const togglePasswordVisibility = () => {
    const input = document.getElementById('password');
    const icon = document.getElementById('pw-toggle-icon');
    const button = document.getElementById('togglePasswordButton');

    if (!(input instanceof HTMLInputElement) || !(icon instanceof HTMLElement) || !(button instanceof HTMLButtonElement)) {
        return;
    }

    const showPassword = input.type === 'password';
    input.type = showPassword ? 'text' : 'password';
    icon.classList.toggle('bi-eye', !showPassword);
    icon.classList.toggle('bi-eye-slash', showPassword);
    button.setAttribute('aria-label', showPassword ? 'Kennwort ausblenden' : 'Kennwort anzeigen');
};

const handleLoginSubmit = async event => {
    event.preventDefault();

    const form = event.currentTarget;
    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    const username = document.getElementById('username')?.value ?? '';
    const password = document.getElementById('password')?.value ?? '';
    const rememberMe = document.getElementById('rememberMe')?.checked ?? false;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    const loginUrl = form.dataset.loginUrl || '/login/index';

    try {
        const response = await fetch(loginUrl, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/json',
                RequestVerificationToken: token
            },
            body: JSON.stringify({ username, password, rememberMe })
        });

        const result = await response.json();

        if (result?.success) {
            window.location.assign(result.redirectUrl || '/Technik/Home/Dashboard');
            return;
        }

        window.alert(result?.message || 'Anmeldung fehlgeschlagen.');
    }
    catch (error) {
        console.error('Login request failed', error);
        window.alert('Fehler bei der Anmeldung. Bitte versuchen Sie es erneut.');
    }
};

document.addEventListener('DOMContentLoaded', initializeLogin);
