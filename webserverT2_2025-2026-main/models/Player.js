const mongoose = require('mongoose');
const bcrypt = require('bcryptjs');

const playerSchema = new mongoose.Schema({
    username: {
        type: String,
        required: [true, 'Username is required'],
        unique: true,
        trim: true,
        minlength: [3, 'Username must be at least 3 characters'],
        maxlength: [20, 'Username cannot exceed 20 characters']
    },
    email: {
        type: String,
        required: [true, 'Email is required'],
        unique: true,
        lowercase: true,
        match: [/^\S+@\S+\.\S+$/, 'Please provide a valid email']
    },
    password:{
        type: String,
        required: [true, 'Password is required'],
        minlength: [8, 'Password must contain at least 8 characters']
    },
    // Updated field
    score: {
        type: Number,
        default: 0,
        min: [0, 'Score cannot be below 0']
    },
}, {
    timestamps: true
});
playerSchema.pre('save', async function(){
    if(!this.isModified('password')){
        return;
    }

    const salt = await bcrypt.genSalt(10);

    this.password = await bcrypt.hash(this.password, salt);

    
});

playerSchema.methods.comparePassword = async function(enteredPassword){
    return await bcrypt.compare(enteredPassword, this.password);
}

const Player = mongoose.model('Player', playerSchema);
module.exports = Player;