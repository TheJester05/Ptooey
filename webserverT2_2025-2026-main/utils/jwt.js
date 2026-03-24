const jwt = require('jsonwebtoken');

const generateToken = (playerId) => {
    return jwt.sign(
        { id: playerId },
        process.env.JWT_SECRET, 
        { expiresIn: process.env.JWT_EXPIRE || '30d' }
    );
};

const verifyToken = (token) => {
    try {
        const decoded = jwt.verify(token, process.env.JWT_SECRET);
        return decoded;
    } catch(error){
        console.error('Token verification error:', error.message)
        return null;
    }
}

module.exports = { generateToken, verifyToken }