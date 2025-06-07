const express = require('express');
const router = express.Router();
const authController = require('./authController'); 
const authMiddleware = require('./authMiddleware');

router.post('/register', authController.register);
router.post('/login', authController.login);

router.post('/verify', authMiddleware, (req, res) => {
    res.status(200).send('Token is valid.');
});

module.exports = router;