const express = require('express');
const mongoose = require('mongoose');
const dotenv = require('dotenv');
const playerRoutes = require('./routes/authRoutes');
const cors = require('cors');
dotenv.config();

const app = express();
app.use(cors({
    origin: '*',
    methods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
    allowedHeaders: ['Content-Type', 'Authorization', 'X-Requested-With', 'Accept']
}));
app.use(express.json());


mongoose.connect(process.env.MONGODB_URI)
    .then(()=> console.log('Connected to MongoDB'))
    .catch((err) => console.error('Failed to Connect to MongoDB: ', err));

const PORT = process.env.PORT || 3000;

app.use('/api/players', playerRoutes);

app.listen(PORT, ()=> {
    console.log(`Server is running on port ${PORT}`)
});

