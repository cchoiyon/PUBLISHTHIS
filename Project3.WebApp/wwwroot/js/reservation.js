// Reservation form handling
document.addEventListener('DOMContentLoaded', function() {
    // Find the submit button and add click handler
    const submitButton = document.getElementById('submitReservation');
    if (submitButton) {
        submitButton.addEventListener('click', handleReservationSubmit);
    }
    
    // Set up the reset button for making another reservation
    const resetButton = document.getElementById('resetReservationButton');
    if (resetButton) {
        resetButton.addEventListener('click', resetReservationForm);
    }
});

// Handle form submission
function handleReservationSubmit() {
    // Show loading spinner and disable button
    const submitButton = document.getElementById('submitReservation');
    const spinner = document.getElementById('submitSpinner');
    
    submitButton.disabled = true;
    spinner.classList.remove('d-none');
    
    // Hide any existing alerts
    document.getElementById('reservationSuccess').classList.add('d-none');
    document.getElementById('reservationError').classList.add('d-none');
    document.getElementById('demoModeAlert').classList.add('d-none');
    document.getElementById('offlineModeAlert').classList.add('d-none');
    
    // Get form data
    const form = document.getElementById('reservationForm');
    const formData = new FormData(form);
    
    // Convert to JSON object
    const jsonData = {};
    formData.forEach((value, key) => {
        jsonData[key] = value;
    });
    
    // Get the anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    // Submit via AJAX
    fetch('/Reservation/CreateAjax', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify(jsonData)
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then(data => {
        // Hide spinner and re-enable button
        submitButton.disabled = false;
        spinner.classList.add('d-none');
        
        if (data.success) {
            // Show success message
            handleSuccessfulReservation(data);
        } else {
            // Show error message
            handleReservationError(data);
        }
    })
    .catch(error => {
        // Hide spinner and re-enable button
        submitButton.disabled = false;
        spinner.classList.add('d-none');
        
        // Show generic error
        console.error('Error submitting reservation:', error);
        document.getElementById('errorMessage').textContent = 'An unexpected error occurred. Please try again later.';
        document.getElementById('reservationError').classList.remove('d-none');
    });
}

// Handle successful reservation
function handleSuccessfulReservation(data) {
    // Hide the form and show success view
    document.getElementById('reservationFormContainer').classList.add('d-none');
    document.getElementById('successView').classList.remove('d-none');
    
    // Hide form buttons and show success buttons
    document.getElementById('formFooterButtons').classList.add('d-none');
    document.getElementById('successFooterButtons').classList.remove('d-none');
    
    // If there's a demo mode message, show it
    if (data.message && data.message.includes('demo mode')) {
        document.getElementById('demoMessage').textContent = data.message;
        document.getElementById('demoModeAlert').classList.remove('d-none');
    }
    
    // If this is an offline mode response
    if (data.isOfflineMode) {
        document.getElementById('offlineMessage').textContent = data.message;
        document.getElementById('offlineModeAlert').classList.remove('d-none');
    }
}

// Handle reservation error
function handleReservationError(data) {
    if (data.errors) {
        // Handle validation errors
        const validationSummary = document.getElementById('validationSummary');
        validationSummary.innerHTML = '';
        
        // Create bullet list for errors
        const errorList = document.createElement('ul');
        errorList.className = 'mt-2 mb-0';
        
        // Add each error to the summary and highlight fields
        Object.keys(data.errors).forEach(key => {
            // Add to error list
            const li = document.createElement('li');
            li.textContent = data.errors[key];
            errorList.appendChild(li);
            
            // Highlight the field
            const field = document.getElementById(key);
            if (field) {
                field.classList.add('is-invalid');
                
                // Add message to field-specific error display
                const errorSpan = document.querySelector(`[data-valmsg-for="${key}"]`);
                if (errorSpan) {
                    errorSpan.textContent = data.errors[key];
                }
            }
        });
        
        validationSummary.appendChild(errorList);
    } else {
        // Show general error message
        document.getElementById('errorMessage').textContent = data.message || 'An error occurred while processing your reservation.';
        document.getElementById('reservationError').classList.remove('d-none');
    }
}

// Reset form for another reservation
function resetReservationForm() {
    // Show the form again
    document.getElementById('reservationFormContainer').classList.remove('d-none');
    document.getElementById('successView').classList.add('d-none');
    
    // Show form buttons and hide success buttons
    document.getElementById('formFooterButtons').classList.remove('d-none');
    document.getElementById('successFooterButtons').classList.add('d-none');
    
    // Hide all alerts
    document.getElementById('reservationSuccess').classList.add('d-none');
    document.getElementById('reservationError').classList.add('d-none');
    document.getElementById('demoModeAlert').classList.add('d-none');
    document.getElementById('offlineModeAlert').classList.add('d-none');
    
    // Clear any validation errors
    document.getElementById('validationSummary').innerHTML = '';
    
    // Reset all error messages
    const errorSpans = document.querySelectorAll('[data-valmsg-for]');
    errorSpans.forEach(span => {
        span.textContent = '';
    });
    
    // Remove invalid class from all inputs
    const inputs = document.querySelectorAll('.is-invalid');
    inputs.forEach(input => {
        input.classList.remove('is-invalid');
    });
} 