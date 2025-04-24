/**
 * This is a utility script to fix the reservation issues in the application.
 * The issue is that the ReservationController has a routing problem:
 * It's defined with [Route("[controller]")] at the controller level
 * but then [Route("Reservation/GetInitialModel")] at the action level
 * which creates a conflict.
 * 
 * To fix this, we need to modify the JavaScript to use the corrected route
 * or use the fallback modal directly.
 */

console.log("Reservation fix utility loaded");

// Override the original loadReservationModal function when the script loads
window.originalLoadReservationModal = window.loadReservationModal;

window.loadReservationModal = function(restaurantId, restaurantName) {
    console.log("FIXED: Loading reservation modal for: " + restaurantName + " (" + restaurantId + ")");
    
    // Try using the corrected API endpoint instead of the problematic one
    const fixedModelUrl = `/Reservation/GetModel?restaurantId=${restaurantId}`;
    console.log("Fetching from fixed URL:", fixedModelUrl);
    
    // Load the reservation modal via AJAX with the fixed URL
    fetch(fixedModelUrl)
        .then(response => {
            console.log("Response status (fixed URL):", response.status);
            if (!response.ok) {
                console.log("Still having issues - using fallback modal");
                createFallbackModal();
                throw new Error('Failed to load reservation form');
            }
            return response.text();
        })
        .then(html => {
            console.log("Success! Received HTML content from fixed URL");
            
            // Insert the modal HTML into the container
            document.getElementById('reservationModalContainer').innerHTML = html;
            
            // Show the modal
            const reservationModal = document.getElementById('reservationModal');
            if (reservationModal) {
                const modal = new bootstrap.Modal(reservationModal);
                modal.show();
                setupModalEventHandlers();
            }
        })
        .catch(error => {
            console.error('Error loading reservation modal:', error);
            // We'll use the fallback modal if needed
            if (!document.getElementById('reservationModal')) {
                createFallbackModal();
            }
        });
    
    // Helper function to create the fallback modal if needed
    function createFallbackModal() {
        console.log("Creating fallback reservation modal");
        
        // Basic HTML for the reservation modal with proper buttons and structure
        const modalHtml = 
        '<div class="modal fade" id="reservationModal" tabindex="-1" aria-labelledby="reservationModalLabel" aria-hidden="true">' +
            '<div class="modal-dialog modal-lg">' +
                '<div class="modal-content">' +
                    '<div class="modal-header">' +
                        '<h5 class="modal-title" id="reservationModalLabel">Make a Reservation at ' + restaurantName + '</h5>' +
                        '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>' +
                    '</div>' +
                    '<div class="modal-body">' +
                        '<div id="reservationFormContainer">' +
                            '<form id="reservationForm" method="post">' +
                                '<div id="validationSummary" class="text-danger mb-3"></div>' +
                                
                                '<input type="hidden" id="RestaurantID" name="RestaurantID" value="' + restaurantId + '" />' +
                                '<input type="hidden" id="RestaurantName" name="RestaurantName" value="' + restaurantName + '" />' +
                                
                                '<div class="mb-3">' +
                                    '<label for="ContactName" class="form-label">Your Name</label>' +
                                    '<input id="ContactName" name="ContactName" class="form-control" placeholder="Your Name" value="" />' +
                                    '<span data-valmsg-for="ContactName" class="text-danger"></span>' +
                                '</div>' +
                                
                                '<div class="mb-3">' +
                                    '<label for="Phone" class="form-label">Phone Number</label>' +
                                    '<input id="Phone" name="Phone" class="form-control" placeholder="Your Phone Number" value="" />' +
                                    '<span data-valmsg-for="Phone" class="text-danger"></span>' +
                                '</div>' +
                                
                                '<div class="mb-3">' +
                                    '<label for="Email" class="form-label">Email Address</label>' +
                                    '<input id="Email" name="Email" type="email" class="form-control" placeholder="Your Email Address" value="" />' +
                                    '<span data-valmsg-for="Email" class="text-danger"></span>' +
                                '</div>' +
                                
                                '<div class="row g-3">' +
                                    '<div class="col-md-6 mb-3">' +
                                        '<label for="ReservationDateTime" class="form-label">Date & Time</label>' +
                                        '<input id="ReservationDateTime" name="ReservationDateTime" type="datetime-local" class="form-control" value="" />' +
                                        '<span data-valmsg-for="ReservationDateTime" class="text-danger"></span>' +
                                    '</div>' +
                                    
                                    '<div class="col-md-6 mb-3">' +
                                        '<label for="PartySize" class="form-label">Party Size</label>' +
                                        '<input id="PartySize" name="PartySize" type="number" min="1" class="form-control" placeholder="Number of Guests" value="2" />' +
                                        '<span data-valmsg-for="PartySize" class="text-danger"></span>' +
                                    '</div>' +
                                '</div>' +
                                
                                '<div class="mb-3">' +
                                    '<label for="SpecialRequests" class="form-label">Special Requests</label>' +
                                    '<textarea id="SpecialRequests" name="SpecialRequests" class="form-control" rows="3" placeholder="Any special requests? (Optional)"></textarea>' +
                                    '<span data-valmsg-for="SpecialRequests" class="text-danger"></span>' +
                                '</div>' +
                            '</form>' +
                        '</div>' +
                        
                        '<!-- Success view (hidden by default) -->' +
                        '<div id="successView" class="d-none text-center">' +
                            '<div class="mb-4">' +
                                '<i class="bi bi-check-circle-fill text-success" style="font-size: 5rem;"></i>' +
                            '</div>' +
                            '<h2 class="mb-3">Reservation Confirmed!</h2>' +
                            '<p class="lead mb-4">Your reservation request has been submitted successfully.</p>' +
                            '<p>The restaurant will contact you if any additional information is needed.</p>' +
                        '</div>' +
                        
                        '<!-- Alert messages -->' +
                        '<div id="reservationSuccess" class="alert alert-success alert-dismissible fade show d-none" role="alert">' +
                            '<strong>Success!</strong> Your reservation has been submitted successfully.' +
                            '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>' +
                        '</div>' +
                        
                        '<div id="reservationError" class="alert alert-danger alert-dismissible fade show d-none" role="alert">' +
                            '<strong>Error!</strong> <span id="errorMessage">There was a problem submitting your reservation.</span>' +
                            '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>' +
                        '</div>' +
                        
                        '<div id="demoModeAlert" class="alert alert-info alert-dismissible fade show d-none" role="alert">' +
                            '<i class="bi bi-info-circle me-2"></i> <span id="demoMessage">This is a demo mode response.</span>' +
                            '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>' +
                        '</div>' +
                        
                        '<div id="offlineModeAlert" class="alert alert-warning alert-dismissible fade show d-none" role="alert">' +
                            '<i class="bi bi-wifi-off me-2"></i> <span id="offlineMessage">Your reservation has been recorded in offline mode.</span>' +
                            '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>' +
                        '</div>' +
                    '</div>' +
                    '<div class="modal-footer">' +
                        '<div id="formFooterButtons">' +
                            '<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>' +
                            '<button id="submitReservation" type="button" class="btn btn-primary">Submit Reservation</button>' +
                            '<div id="submitSpinner" class="spinner-border text-primary d-none" role="status">' +
                                '<span class="visually-hidden">Loading...</span>' +
                            '</div>' +
                        '</div>' +
                        '<div id="successFooterButtons" class="d-none">' +
                            '<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>' +
                            '<button id="resetReservationButton" type="button" class="btn btn-primary">Make Another Reservation</button>' +
                        '</div>' +
                    '</div>' +
                '</div>' +
            '</div>' +
        '</div>';
        
        // Insert the fallback modal HTML
        document.getElementById('reservationModalContainer').innerHTML = modalHtml;
        
        // Set a default date/time for the reservation
        const today = new Date();
        const tomorrow = new Date(today);
        tomorrow.setDate(tomorrow.getDate() + 1);
        tomorrow.setHours(19, 0, 0, 0);
        
        // Format date for datetime-local input (YYYY-MM-DDTHH:MM)
        const formattedDate = tomorrow.toISOString().slice(0, 16);
        const dateTimeInput = document.getElementById('ReservationDateTime');
        if (dateTimeInput) {
            dateTimeInput.value = formattedDate;
        }
        
        // Show the modal
        const reservationModal = document.getElementById('reservationModal');
        if (reservationModal) {
            try {
                const modal = new bootstrap.Modal(reservationModal);
                modal.show();
            } catch (err) {
                console.error("Error showing fallback modal:", err);
            }
        }

        // Set up the event handlers
        setupModalEventHandlers();
    }
};

// Tell the developer that we're ready
console.log("Reservation fix script ready - the system will now use the fixed URL"); 