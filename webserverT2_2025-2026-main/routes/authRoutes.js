
const express = require('express');
const router = express.Router();
const {
    createPlayer,
    login,
    updateScore
} = require('../controllers/playerController');
const { protect } = require('../middleware/auth');

router.put('/score', protect, updateScore);
router.post('/register', createPlayer);
router.post('/login', login);

module.exports = router;