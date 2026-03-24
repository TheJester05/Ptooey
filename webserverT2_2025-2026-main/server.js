const express = require('express');
const mongoose = require('mongoose');
const dotenv = require('dotenv');
const playerRoutes = require('./routes/authRoutes');

dotenv.config();

const app = express();
app.use(express.json());


mongoose.connect(process.env.MONGODB_URI)
    .then(()=> console.log('Connected to MongoDB'))
    .catch((err) => console.error('Failed to Connect to MongoDB: ', err));

const PORT = process.env.PORT || 3000;

app.use('/api/players', playerRoutes);

app.listen(PORT, ()=> {
    console.log(`Server is running on port ${PORT}`)
});

