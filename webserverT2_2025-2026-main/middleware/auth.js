const {verifyToken } = require('../utils/jwt');
const Player = require('../models/Player');

exports.protect = async(req, res, next) =>{
    let token;

    if(req.headers.authorization && req.headers.authorization.startsWith('Bearer')){
        try {
            token = req.headers.authorization.split(' ')[1];
            if(!token || token === 'undefined' || token === 'null'){
                return res.status(401).json({
                    success: false,
                    message: 'Token is malformed. Format should be: Bearer <token>'
                });
            }
        } catch(error){
            return res.status(401).json({
                success: false,
                message: 'Error Extracting token from Authorization'
            });
        }
    }

    if(!token){
        return res.status(401).json({
            success: false,
            message: 'Error Extracting token from Authorization'
        });
    }

    try {
        const decoded = verifyToken(token);

        if(!decoded){
            return res.status(401).json({
                success: false,
                message: 'Invalid or expired token. Please Login again'
            });
        }

        const player = await Player.findById(decoded.id).select('-password');

        if(!player){
            return res.status(404).json({
                success: false,
                message: 'Player not found'
            });
        }

        req.player = player;

        console.log('verified user going next');
        next(); 


    } catch(error){
        return res.status(401).json({
            success: false,
            message: 'Not Authorized',
            error: error.message
        });
    }
}