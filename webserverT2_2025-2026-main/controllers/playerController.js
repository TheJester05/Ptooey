const Player = require('../models/Player');
const {generateToken } = require('../utils/jwt');


exports.createPlayer = async (req, res) => {
    try {
        const player = await Player.create(req.body);

        const token = generateToken(player._id);

        res.status(201).json({
            success: true,
            message: 'Player registered Successfully',
            token,
            data: {
                id: player._id,
                username: player.username,
                email: player.email,
                score: player.score
            }
        });
    } catch (error) {

        if(error.name === 'ValidationError'){
            const messages = Object.values(error.errors).map(err => err.message);
            return res.status(400).json({
                success: false,
                message: 'Validation Error',
                error: messages
            });
        }

        if(error.code === 11000){
            const field = Object.keys(error.keyPattern)[0];

            return res.status(400).json({
                success: false,
                message: `${field} already exists`
            });
        }

        res.status(500).json({
            success: false,
            message: 'Server Error',
            error: error.message
        })
    }
}

exports.login = async (req, res) => {
    try {
        const {username, password} = req.body;

        if(!username || !password){
            return res.status(400).json({
                success : false,
                message: "Please provide a valid username and password"
            });
        }

        const player = await Player.findOne({username});
        if(!player || player.password !== password){
            return res.status(400).json({
                success : false,
                message: "Invalid username or password"
            });
        }

        
        const token = generateToken(player._id);

        res.status(201).json({
            success: true,
            message: 'Login Successful',
            token,
            data: {
                id: player._id,
                username: player.username,
                email: player.email,
                kills: player.kills,
                deaths: player.deaths
            }
        });

    } 
    catch(error) {
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
}

exports.updateScore = async(req, res) => {
    try {
        const { score } = req.body; 
        const player = req.player;

        if(!player){
            return res.status(404).json({
                success: false,
                message: 'Player not found'
            });
        }

        
        player.score = player.score + score;

        await player.save();

        res.status(200).json({
            success: true,
            data: player
        });
    }
    catch (error){
        res.status(500).json({
            success: false,
            message: 'Failed to Update Score',
            error: error.message
        });
    }
}