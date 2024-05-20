let mainImage = document.getElementById('mainImage');
let thumbnails = document.querySelectorAll('.thumbnail-carousel img');
let next = document.getElementById('next');
let prev = document.getElementById('prev');
let dots = document.querySelectorAll('.dots img');

let currentIndex = 0;
let images = Array.from(thumbnails).map(thumbnail => thumbnail.src);

next.onclick = function() {
    currentIndex = (currentIndex + 1) % images.length;
    updateMainImage(images[currentIndex]);
    updateDots();
}

prev.onclick = function() {
    currentIndex = (currentIndex - 1 + images.length) % images.length;
    updateMainImage(images[currentIndex]);
    updateDots();
}

thumbnails.forEach((thumbnail, index) => {
    thumbnail.onclick = function() {
        updateMainImage(images[index]);
        currentIndex = index;
        updateDots();
    }
});

dots.forEach((dot, index) => {
    dot.onclick = function() {
        updateMainImage(images[index]);
        currentIndex = index;
        updateDots();
    }
});

function updateMainImage(src) {
    mainImage.src = src;
}

function updateDots() {
    dots.forEach((dot, index) => {
        dot.classList.toggle('active', index === currentIndex);
    });
}

// Initialize slider
updateMainImage(images[currentIndex]);
updateDots();
