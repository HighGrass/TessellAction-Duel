const express = require('express');
const router = express.Router();
const authController = require('./authController');
const authMiddleware = require('./authMiddleware');

router.post('/register', authController.register);
router.post('/login', authController.login);

router.post('/verify', authMiddleware, (req, res) => {
    res.status(200).send('Token is valid.');
});

router.get('/me', authMiddleware, (req, res) => {
    //uthMiddleware já verificou o token 
    res.json({
        userId: req.user._id,
        username: req.user.username,
        globalScore: req.user.globalScore,
        gamesPlayed: req.user.gamesPlayed,
        gamesWon: req.user.gamesWon
    });
});

module.exports = router;